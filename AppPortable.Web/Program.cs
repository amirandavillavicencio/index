using AppPortable.Core.Interfaces;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;
using AppPortable.Web.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSingleton<ILocalStorageService>(_ =>
{
    var configuredRoot = builder.Configuration["Storage:RootPath"];

    var rootPath = !string.IsNullOrWhiteSpace(configuredRoot)
        ? configuredRoot
        : Path.Combine(AppContext.BaseDirectory, "AppData");

    return new LocalStorageService(rootPath);
});

builder.Services.AddSingleton<IPdfExtractionService, PdfExtractionService>();
builder.Services.AddSingleton<IOcrService, TesseractOcrService>();
builder.Services.AddSingleton<IChunkingService, ParagraphChunkingService>();
builder.Services.AddSingleton<IJsonPersistenceService, JsonPersistenceService>();
builder.Services.AddSingleton<IIndexService, SqliteIndexService>();
builder.Services.AddSingleton<ISearchService, SqliteSearchService>();
builder.Services.AddSingleton<IDocumentProcessor, InfrastructureDocumentProcessor>();

var app = builder.Build();

app.UseDefaultFiles();
app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapPost("/api/documents", async (
    IFormFile file,
    IDocumentProcessor processor,
    CancellationToken cancellationToken) =>
{
    if (file.Length == 0)
    {
        return Results.BadRequest(new { error = "El archivo está vacío." });
    }

    if (!string.Equals(Path.GetExtension(file.FileName), ".pdf", StringComparison.OrdinalIgnoreCase))
    {
        return Results.BadRequest(new { error = "Solo se aceptan archivos PDF." });
    }

    var tempFile = Path.Combine(Path.GetTempPath(), $"appportable-web-{Guid.NewGuid():N}.pdf");

    try
    {
        await using (var stream = File.Create(tempFile))
        {
            await file.CopyToAsync(stream, cancellationToken);
        }

        var processed = await processor.ProcessAsync(tempFile, enableOcrFallback: true, cancellationToken);

        var response = new ProcessDocumentResponse(
            processed.DocumentId,
            processed.SourceFile,
            processed.TotalPages,
            processed.Chunks.Count,
            processed.ExtractionSummary,
            processed.Warnings);

        return Results.Ok(response);
    }
    finally
    {
        if (File.Exists(tempFile))
        {
            File.Delete(tempFile);
        }
    }
})
.DisableAntiforgery()
.Accepts<IFormFile>("multipart/form-data")
.WithName("ProcessDocument")
.WithTags("Documents");

app.MapPost("/api/search", async (
    SearchRequest request,
    ISearchService searchService,
    CancellationToken cancellationToken) =>
{
    if (string.IsNullOrWhiteSpace(request.Query))
    {
        return Results.BadRequest(new { error = "La consulta no puede estar vacía." });
    }

    var limit = request.Limit <= 0 ? 20 : Math.Min(request.Limit, 100);
    var results = await searchService.SearchAsync(request.Query, limit, cancellationToken);
    return Results.Ok(new SearchResponse(results));
})
.WithName("Search")
.WithTags("Search");

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }))
   .WithName("Health")
   .WithTags("System");

app.Run();
