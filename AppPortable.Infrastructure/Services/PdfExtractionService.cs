using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text;

namespace AppPortable.Infrastructure.Services;

public sealed class PdfExtractionService : IPdfExtractionService
{
    public Task<IReadOnlyList<DocumentPage>> ExtractPagesAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        var pages = new List<DocumentPage>();

        var readerProperties = new ReaderProperties();
        using var reader = new PdfReader(pdfPath, readerProperties);
        reader.SetUnethicalReading(true);
        using var pdf = new PdfDocument(reader);

        for (var pageNumber = 1; pageNumber <= pdf.GetNumberOfPages(); pageNumber++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var page = pdf.GetPage(pageNumber);
            var strategy = new LocationTextExtractionStrategy();
            var rawText = PdfTextExtractor.GetTextFromPage(page, strategy) ?? string.Empty;

            // Corregir encoding Latin-1 mal interpretado como UTF-8
            var text = FixEncoding(rawText).Trim();

            var warnings = text.Length == 0
                ? new[] { "No se detecto texto nativo en la pagina. OCR no configurado." }
                : Array.Empty<string>();

            pages.Add(new DocumentPage
            {
                PageNumber = pageNumber,
                Text = text,
                TextLength = text.Length,
                ExtractionLayer = text.Length > 0 ? ExtractionLayer.Native : ExtractionLayer.Failed,
                OcrConfidence = null,
                Warnings = warnings
            });
        }

        return Task.FromResult<IReadOnlyList<DocumentPage>>(pages);
    }

    private static string FixEncoding(string input)
    {
        try
        {
            // Detectar si el texto viene como Latin-1 mal interpretado
            var latin1Bytes = Encoding.Latin1.GetBytes(input);
            var utf8Text = Encoding.UTF8.GetString(latin1Bytes);

            // Validar que la conversion mejoro el texto (sin caracteres de reemplazo)
            if (!utf8Text.Contains('\uFFFD') && utf8Text != input)
                return utf8Text.Normalize(NormalizationForm.FormC);
        }
        catch { }

        return input.Normalize(NormalizationForm.FormC);
    }
}
