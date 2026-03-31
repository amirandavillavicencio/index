using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IJsonPersistenceService
{
    Task SaveDocumentAsync(string path, ProcessedDocument document, CancellationToken cancellationToken = default);
    Task SaveChunksAsync(string path, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task<ProcessedDocument?> LoadDocumentAsync(string path, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentChunk>> LoadChunksAsync(string path, CancellationToken cancellationToken = default);
}
