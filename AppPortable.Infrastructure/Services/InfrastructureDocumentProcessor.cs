using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using AppPortable.Core.Services;

namespace AppPortable.Infrastructure.Services;

public sealed class InfrastructureDocumentProcessor : IDocumentProcessor
{
    private readonly DocumentProcessor _processor;

    public InfrastructureDocumentProcessor(
        ILocalStorageService localStorageService,
        IPdfExtractionService pdfExtractionService,
        IOcrService ocrService,
        IChunkingService chunkingService,
        IJsonPersistenceService jsonPersistenceService,
        IIndexService indexService)
    {
        _processor = new DocumentProcessor(
            localStorageService,
            pdfExtractionService,
            ocrService,
            chunkingService,
            jsonPersistenceService,
            indexService);
    }

    public Task<ProcessedDocument> ProcessAsync(string sourcePdfPath, bool enableOcrFallback = true, CancellationToken cancellationToken = default)
        => _processor.ProcessAsync(sourcePdfPath, enableOcrFallback, cancellationToken);
}
