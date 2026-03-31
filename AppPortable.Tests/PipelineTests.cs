using AppPortable.Core.Interfaces;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;
using AppPortable.Tests.TestHelpers;

namespace AppPortable.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task ProcessAsync_ExecutesFullPipeline()
    {
        var root = Path.Combine(Path.GetTempPath(), $"pipeline_{Guid.NewGuid():N}");
        ILocalStorageService storage = new LocalStorageService(root);
        var json = new JsonPersistenceService();
        var extractor = new PdfExtractionService();
        var chunking = new ParagraphChunkingService(300, 80);
        var index = new SqliteIndexService(storage);
        var processor = new InfrastructureDocumentProcessor(storage, extractor, new TesseractOcrService(), chunking, json, index);
        var search = new SqliteSearchService(storage, index);
        var pdf = TestPdfFactory.CreatePdfWithPages("contenido de prueba para pipeline y búsqueda");

        var processed = await processor.ProcessAsync(pdf);
        var results = await search.SearchAsync("pipeline");

        Assert.NotEmpty(processed.DocumentId);
        Assert.True(File.Exists(storage.GetDocumentJsonPath(processed.DocumentId)));
        Assert.NotEmpty(processed.Chunks);
        Assert.NotEmpty(results);
    }
}
