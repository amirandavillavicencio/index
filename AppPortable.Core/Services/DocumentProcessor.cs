using System.Security.Cryptography;
using System.Text;
using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Core.Services;

public sealed class DocumentProcessor(
    ILocalStorageService localStorageService,
    IPdfExtractionService pdfExtractionService,
    IOcrService ocrService,
    IChunkingService chunkingService,
    IJsonPersistenceService jsonPersistenceService,
    IIndexService indexService) : IDocumentProcessor
{
    public async Task<ProcessedDocument> ProcessAsync(string sourcePdfPath, bool enableOcrFallback = true, CancellationToken cancellationToken = default)
    {
        localStorageService.EnsureInitialized();
        await indexService.EnsureInitializedAsync(cancellationToken);

        var copiedSourcePath = await localStorageService.CopySourceDocumentAsync(sourcePdfPath, cancellationToken);
        var extractedPages = await pdfExtractionService.ExtractPagesAsync(copiedSourcePath, cancellationToken);
        var pages = await ApplyOcrFallbackAsync(copiedSourcePath, extractedPages, enableOcrFallback, cancellationToken);

        var documentId = ComputeDocumentId(copiedSourcePath, pages);
        var chunks = chunkingService.CreateChunks(documentId, copiedSourcePath, pages);

        var processedDocument = new ProcessedDocument
        {
            DocumentId = documentId,
            SourceFile = copiedSourcePath,
            ProcessedAt = DateTime.UtcNow,
            TotalPages = pages.Count,
            Pages = pages,
            Chunks = chunks,
            ExtractionSummary = BuildSummary(pages),
            Warnings = BuildWarnings(pages)
        };

        var documentJsonPath = localStorageService.GetDocumentJsonPath(documentId);
        var chunksJsonPath = localStorageService.GetChunksJsonPath(documentId);

        await jsonPersistenceService.SaveDocumentAsync(documentJsonPath, processedDocument, cancellationToken);
        await jsonPersistenceService.SaveChunksAsync(chunksJsonPath, processedDocument.Chunks, cancellationToken);
        await indexService.IndexChunksAsync(documentId, processedDocument.Chunks, cancellationToken);

        return processedDocument;
    }

    private async Task<IReadOnlyList<DocumentPage>> ApplyOcrFallbackAsync(
        string sourcePdfPath,
        IReadOnlyList<DocumentPage> pages,
        bool enableOcrFallback,
        CancellationToken cancellationToken)
    {
        if (!enableOcrFallback || !ocrService.IsAvailable)
        {
            return pages;
        }

        var requiresOcr = pages.Any(static p => p.ExtractionLayer is ExtractionLayer.Failed || p.TextLength == 0);
        if (!requiresOcr)
        {
            return pages;
        }

        return await ocrService.ApplyOcrAsync(sourcePdfPath, pages, cancellationToken);
    }

    private static string ComputeDocumentId(string sourcePath, IReadOnlyList<DocumentPage> pages)
    {
        var payload = $"{Path.GetFileName(sourcePath)}|{new FileInfo(sourcePath).Length}|{pages.Count}|{pages.Sum(p => p.TextLength)}";
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(hash).ToLowerInvariant()[..24];
    }

    private static ExtractionSummary BuildSummary(IReadOnlyList<DocumentPage> pages)
    {
        return new ExtractionSummary
        {
            Native = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Native),
            Ocr = pages.Count(p => p.ExtractionLayer is ExtractionLayer.Ocr or ExtractionLayer.Mixed),
            Failed = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Failed)
        };
    }

    private static IReadOnlyList<string> BuildWarnings(IReadOnlyList<DocumentPage> pages)
    {
        return pages
            .Where(p => p.Warnings.Count > 0)
            .SelectMany(p => p.Warnings.Select(w => $"Page {p.PageNumber}: {w}"))
            .Distinct()
            .ToArray();
    }
}
