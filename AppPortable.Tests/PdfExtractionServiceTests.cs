using AppPortable.Infrastructure.Services;
using AppPortable.Tests.TestHelpers;

namespace AppPortable.Tests;

public sealed class PdfExtractionServiceTests
{
    [Fact]
    public async Task ExtractPagesAsync_ExtractsTextAndFlagsEmptyPages()
    {
        var pdfPath = TestPdfFactory.CreatePdfWithPages("Hola mundo en página 1", "");
        var sut = new PdfExtractionService();

        var pages = await sut.ExtractPagesAsync(pdfPath);

        Assert.Equal(2, pages.Count);
        Assert.Contains("Hola mundo", pages[0].Text);
        Assert.Equal(0, pages[1].TextLength);
        Assert.NotEmpty(pages[1].Warnings);
    }
}
