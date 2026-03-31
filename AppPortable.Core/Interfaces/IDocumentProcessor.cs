using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IDocumentProcessor
{
    Task<ProcessedDocument> ProcessAsync(string sourcePdfPath, bool enableOcrFallback = true, CancellationToken cancellationToken = default);
}
