using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;

namespace AppPortable.Tests.TestHelpers;

internal static class TestPdfFactory
{
    public static string CreatePdfWithPages(params string[] pageTexts)
    {
        var path = Path.Combine(Path.GetTempPath(), $"test_{Guid.NewGuid():N}.pdf");
        using var writer = new PdfWriter(path);
        using var pdf = new PdfDocument(writer);
        using var document = new Document(pdf);

        for (var i = 0; i < pageTexts.Length; i++)
        {
            if (i > 0)
                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));

            // ✅ Siempre agregar algo — página vacía causa NullRef en Flush
            var content = string.IsNullOrWhiteSpace(pageTexts[i]) ? " " : pageTexts[i];
            document.Add(new Paragraph(content));
        }

        // ✅ Eliminar document.Flush() — el using lo maneja automáticamente
        return path;
    }
}