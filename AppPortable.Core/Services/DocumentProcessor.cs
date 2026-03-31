using System.Security.Cryptography;
using System.Text;
using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Core.Services;

public sealed class DocumentProcessor(
    ILocalStorageService localStorageService,
    IPdfExtractionService pdfExtractionService,
    IChunkingService chunkingService,
    IJsonPersistenceService jsonPersistenceService,
    IIndexService indexService) : IDocumentProcessor
{
    public async Task<ProcessedDocument> ProcessAsync(string sourcePdfPath, CancellationToken cancellationToken = default)
    {
        localStorageService.EnsureInitialized();
        await indexService.EnsureInitializedAsync(cancellationToken);

        var copiedSourcePath = await localStorageService.CopySourceDocumentAsync(sourcePdfPath, cancellationToken);
        var pages = await pdfExtractionService.ExtractPagesAsync(copiedSourcePath, cancellationToken);
        var documentId = ComputeDocumentId(copiedSourcePath, pages);

        var processedDocument = new ProcessedDocument
        {
            DocumentId = documentId,
            SourceFile = copiedSourcePath,
            ProcessedAt = DateTimeOffset.UtcNow,
            TotalPages = pages.Count,
            Pages = pages.ToList(),
            ExtractionSummary = BuildSummary(pages),
            Warnings = BuildWarnings(pages)
        };

        processedDocument.Chunks = chunkingService
            .CreateChunks(documentId, copiedSourcePath, pages)
            .ToList();

        var documentJsonPath = localStorageService.GetDocumentJsonPath(documentId);
        var chunksJsonPath = localStorageService.GetChunksJsonPath(documentId);

        await jsonPersistenceService.SaveAsync(documentJsonPath, processedDocument, cancellationToken);
        await jsonPersistenceService.SaveAsync(chunksJsonPath, processedDocument.Chunks, cancellationToken);
        await indexService.IndexDocumentAsync(processedDocument, cancellationToken);

        return processedDocument;
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
            Ocr = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Ocr || p.ExtractionLayer == ExtractionLayer.Mixed),
            Failed = pages.Count(p => p.ExtractionLayer == ExtractionLayer.Failed)
        };
    }

    private static List<string> BuildWarnings(IReadOnlyList<DocumentPage> pages)
    {
        return pages
            .Where(p => p.Warnings.Count > 0)
            .SelectMany(p => p.Warnings.Select(w => $"Page {p.PageNumber}: {w}"))
            .Distinct()
            .ToList();
    }
}
