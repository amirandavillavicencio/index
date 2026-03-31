using System.Text.Json;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Services;

public sealed class JsonPersistenceService : IJsonPersistenceService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        WriteIndented = true
    };

    public Task SaveDocumentAsync(string path, ProcessedDocument document, CancellationToken cancellationToken = default)
        => SaveAsync(path, document, cancellationToken);

    public Task SaveChunksAsync(string path, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
        => SaveAsync(path, chunks, cancellationToken);

    public async Task<ProcessedDocument?> LoadDocumentAsync(string path, CancellationToken cancellationToken = default)
        => await LoadAsync<ProcessedDocument>(path, cancellationToken);

    public async Task<IReadOnlyList<DocumentChunk>> LoadChunksAsync(string path, CancellationToken cancellationToken = default)
        => await LoadAsync<List<DocumentChunk>>(path, cancellationToken) ?? [];

    public async Task SaveAsync<T>(string path, T data, CancellationToken cancellationToken = default)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, data, Options, cancellationToken);
    }

    public async Task<T?> LoadAsync<T>(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
        {
            return default;
        }

        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<T>(stream, Options, cancellationToken);
    }
}
