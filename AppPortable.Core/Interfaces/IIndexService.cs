using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IIndexService
{
    Task EnsureInitializedAsync(CancellationToken cancellationToken = default);
    Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default);
    Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default);
}
