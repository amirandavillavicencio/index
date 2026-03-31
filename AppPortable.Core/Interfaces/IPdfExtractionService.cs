using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IPdfExtractionService
{
    Task<IReadOnlyList<DocumentPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken = default);
}
