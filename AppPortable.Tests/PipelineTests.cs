using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;
using AppPortable.Tests.TestHelpers;

namespace AppPortable.Tests;

public sealed class PipelineTests
{
    [Fact]
    public async Task ProcessAsync_WithRealServices_PersistsAndIndexesDocument()
    {
        using var tempDir = new TemporaryDirectory("pipeline_full");
        ILocalStorageService storage = new LocalStorageService(tempDir.Path);
        var json = new JsonPersistenceService();
        var extractor = new PdfExtractionService();
        var chunking = new ParagraphChunkingService(280, 80);
        var index = new SqliteIndexService(storage);
        var processor = new InfrastructureDocumentProcessor(storage, extractor, new TesseractOcrService(), chunking, json, index);
        var search = new SqliteSearchService(storage, index);

        var pdf = TestPdfFactory.CreatePdfWithPages(
            "pipeline documental con persistencia json e indexación sqlite para consultas útiles",
            "segunda página con términos de búsqueda y chunking por párrafos");

        var processed = await processor.ProcessAsync(pdf);

        Assert.NotEmpty(processed.DocumentId);
        Assert.Equal(2, processed.TotalPages);
        Assert.NotEmpty(processed.Pages);
        Assert.NotEmpty(processed.Chunks);

        var documentJsonPath = storage.GetDocumentJsonPath(processed.DocumentId);
        var chunksJsonPath = storage.GetChunksJsonPath(processed.DocumentId);

        Assert.True(File.Exists(documentJsonPath));
        Assert.True(File.Exists(chunksJsonPath));

        var results = await search.SearchAsync("sqlite");
        Assert.NotEmpty(results);
        Assert.Contains(results, r => r.DocumentId == processed.DocumentId);
    }

    [Fact]
    public async Task ProcessAsync_WhenOcrFallbackCannotRecoverText_KeepsPipelineStableWithWarnings()
    {
        using var tempDir = new TemporaryDirectory("pipeline_ocr_fallback");
        ILocalStorageService storage = new LocalStorageService(tempDir.Path);
        var json = new JsonPersistenceService();
        var extractor = new PdfExtractionService();
        var chunking = new ParagraphChunkingService(200, 50);
        var index = new SqliteIndexService(storage);
        var failingOcr = new AlwaysAvailableNoOpOcrService();
        var processor = new InfrastructureDocumentProcessor(storage, extractor, failingOcr, chunking, json, index);

        var pdf = TestPdfFactory.CreatePdfWithPages(string.Empty);

        var processed = await processor.ProcessAsync(pdf, enableOcrFallback: true);

        Assert.Single(processed.Pages);
        Assert.Equal(ExtractionLayer.Failed, processed.Pages[0].ExtractionLayer);
        Assert.Contains(processed.Pages[0].Warnings, w => w.Contains("OCR fallback no recuperó texto", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, processed.ExtractionSummary.Failed);
        Assert.Empty(processed.Chunks);
        Assert.Contains(processed.Warnings, w => w.Contains("Page 1", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class AlwaysAvailableNoOpOcrService : IOcrService
    {
        public bool IsAvailable => true;

        public Task<DocumentPage> ApplyOcrAsync(string sourcePdfPath, DocumentPage page, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var warnings = page.Warnings.ToList();
            warnings.Add("OCR fallback no recuperó texto.");

            return Task.FromResult(new DocumentPage
            {
                PageNumber = page.PageNumber,
                ExtractionLayer = page.ExtractionLayer,
                OcrConfidence = 0,
                Text = page.Text,
                TextLength = page.TextLength,
                Warnings = warnings
            });
        }

        public async Task<IReadOnlyList<DocumentPage>> ApplyOcrAsync(string sourcePdfPath, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken = default)
        {
            var output = new List<DocumentPage>(pages.Count);
            foreach (var page in pages)
            {
                output.Add(await ApplyOcrAsync(sourcePdfPath, page, cancellationToken));
            }

            return output;
        }
    }
}
