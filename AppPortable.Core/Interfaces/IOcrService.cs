using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IOcrService
{
    bool IsAvailable { get; }
    Task<DocumentPage> ApplyOcrAsync(string sourcePdfPath, DocumentPage page, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DocumentPage>> ApplyOcrAsync(string sourcePdfPath, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken = default);
}
