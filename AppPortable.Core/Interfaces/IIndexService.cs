using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IIndexService
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    Task IndexChunksAsync(string documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default);
    Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default);
}
