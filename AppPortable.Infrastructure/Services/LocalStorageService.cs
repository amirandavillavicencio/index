using AppPortable.Core.Interfaces;

namespace AppPortable.Infrastructure.Services;

public sealed class LocalStorageService : ILocalStorageService
{
    public string RootPath { get; }
    public string DocumentsPath => Path.Combine(RootPath, "documents");
    public string JsonPath => Path.Combine(RootPath, "json");
    public string ChunksPath => Path.Combine(RootPath, "chunks");
    public string IndexPath => Path.Combine(RootPath, "index");
    public string TempPath => Path.Combine(RootPath, "temp");
    public string LogsPath => Path.Combine(RootPath, "logs");
    public string DatabasePath => Path.Combine(IndexPath, "appportable.db");

    public LocalStorageService(string? rootPath = null)
    {
        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        RootPath = rootPath ?? Path.Combine(localAppData, "AppPortable");
    }

    public void EnsureInitialized()
    {
        Directory.CreateDirectory(RootPath);
        Directory.CreateDirectory(DocumentsPath);
        Directory.CreateDirectory(JsonPath);
        Directory.CreateDirectory(ChunksPath);
        Directory.CreateDirectory(IndexPath);
        Directory.CreateDirectory(TempPath);
        Directory.CreateDirectory(LogsPath);
    }

    public async Task<string> CopySourceDocumentAsync(string sourcePdfPath, CancellationToken cancellationToken = default)
    {
        EnsureInitialized();
        var extension = Path.GetExtension(sourcePdfPath);
        var destinationFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(DocumentsPath, destinationFileName);

        await using var source = File.OpenRead(sourcePdfPath);
        await using var target = File.Create(destinationPath);
        await source.CopyToAsync(target, cancellationToken);

        return destinationPath;
    }

    public string GetDocumentJsonPath(string documentId) => Path.Combine(JsonPath, $"{documentId}.json");

    public string GetChunksJsonPath(string documentId) => Path.Combine(ChunksPath, $"{documentId}.chunks.json");

    public string GetIndexPath(string indexName) => Path.Combine(IndexPath, indexName);

    public string GetTempFilePath(string fileName) => Path.Combine(TempPath, fileName);

    public string GetLogFilePath(DateTime dateUtc) => Path.Combine(LogsPath, $"{dateUtc:yyyyMMdd}.log");
}
