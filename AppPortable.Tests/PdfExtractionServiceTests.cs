using AppPortable.Core.Enums;
using AppPortable.Infrastructure.Services;
using AppPortable.Tests.TestHelpers;

namespace AppPortable.Tests;

public sealed class PdfExtractionServiceTests
{
    [Fact]
    public async Task ExtractPagesAsync_ExtractsNativeTextAndMarksEmptyPageAsFailed()
    {
        var pdfPath = TestPdfFactory.CreatePdfWithPages("Hola mundo en página 1", "");
        var sut = new PdfExtractionService();

        try
        {
            var pages = await sut.ExtractPagesAsync(pdfPath);

            Assert.Equal(2, pages.Count);
            Assert.Contains("Hola mundo", pages[0].Text);
            Assert.Equal(ExtractionLayer.Native, pages[0].ExtractionLayer);

            Assert.Equal(0, pages[1].TextLength);
            Assert.Equal(ExtractionLayer.Failed, pages[1].ExtractionLayer);
            Assert.NotEmpty(pages[1].Warnings);
        }
        finally
        {
            File.Delete(pdfPath);
        }
    }
}
