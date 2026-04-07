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
        RootPath = rootPath ?? PortablePathResolver.ResolveStorageRootPath();
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
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePdfPath);

        EnsureInitialized();

        var extension = Path.GetExtension(sourcePdfPath);
        var destinationFileName = $"{DateTimeOffset.UtcNow:yyyyMMddHHmmssfff}_{Guid.NewGuid():N}{extension}";
        var destinationPath = Path.Combine(DocumentsPath, destinationFileName);

        await using var source = File.OpenRead(sourcePdfPath);
        await using var target = File.Create(destinationPath);
        await source.CopyToAsync(target, cancellationToken);

        return destinationPath;
    }

    public string GetDocumentJsonPath(string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        EnsureInitialized();
        return Path.Combine(JsonPath, $"{documentId}.json");
    }

    public string GetChunksJsonPath(string documentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        EnsureInitialized();
        return Path.Combine(ChunksPath, $"{documentId}.chunks.json");
    }

    public string GetIndexPath(string indexName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(indexName);
        EnsureInitialized();
        return Path.Combine(IndexPath, indexName);
    }

    public string GetTempFilePath(string fileName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(fileName);
        EnsureInitialized();
        return Path.Combine(TempPath, fileName);
    }

    public string GetLogFilePath(DateTime dateUtc)
    {
        EnsureInitialized();
        return Path.Combine(LogsPath, $"appportable-{dateUtc:yyyyMMdd}.log");
    }
}