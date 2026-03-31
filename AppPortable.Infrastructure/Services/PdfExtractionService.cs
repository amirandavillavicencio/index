using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;

namespace AppPortable.Infrastructure.Services;

public sealed class PdfExtractionService : IPdfExtractionService
{
    public Task<IReadOnlyList<DocumentPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        var pages = new List<DocumentPage>();

        using var reader = new PdfReader(pdfPath);
        using var pdf = new PdfDocument(reader);

        for (var pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = pdf.GetPage(pageNumber);
            var strategy = new SimpleTextExtractionStrategy();
            var text = PdfTextExtractor.GetTextFromPage(page, strategy)?.Trim() ?? string.Empty;

            var model = new DocumentPage
            {
                PageNumber = pageNumber,
                Text = text,
                TextLength = text.Length,
                ExtractionLayer = text.Length > 0 ? ExtractionLayer.Native : ExtractionLayer.Failed,
                OcrConfidence = null
            };

            if (text.Length == 0)
            {
                model.Warnings.Add("No se detectó texto nativo en la página. OCR no configurado.");
            }

            pages.Add(model);
        }

        return Task.FromResult<IReadOnlyList<DocumentPage>>(pages);
    }
}
