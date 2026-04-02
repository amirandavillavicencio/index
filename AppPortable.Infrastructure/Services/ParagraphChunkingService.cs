using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Services;

public sealed class ParagraphChunkingService : IChunkingService
{
    private readonly int _targetChars;
    private readonly int _minimumChars;

    public ParagraphChunkingService(int targetChars = 1200, int minimumChars = 60)
    {
        _targetChars = targetChars;
        _minimumChars = minimumChars;
    }

    public IReadOnlyList<DocumentChunk> CreateChunks(ProcessedDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return CreateChunks(document.DocumentId, document.SourceFile, document.Pages);
    }

    public IReadOnlyList<DocumentChunk> CreateChunks(string documentId, string sourceFile, IReadOnlyList<DocumentPage> pages)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceFile);
        ArgumentNullException.ThrowIfNull(pages);

        var chunks = new List<DocumentChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
                continue;

            var text = page.Text.Trim();
            if (text.Length == 0)
                continue;

            chunks.Add(new DocumentChunk
            {
                ChunkId = $"{documentId}-{chunkIndex:D4}",
                DocumentId = documentId,
                SourceFile = sourceFile,
                PageStart = page.PageNumber,
                PageEnd = page.PageNumber,
                ChunkIndex = chunkIndex,
                Text = text,
                TextLength = text.Length,
                ExtractionLayersInvolved = new[] { page.ExtractionLayer },
                Metadata = new Dictionary<string, string>
                {
                    ["strategy"] = "one_page_one_chunk",
                    ["target_chars"] = _targetChars.ToString(),
                    ["minimum_chars"] = _minimumChars.ToString()
                }
            });

            chunkIndex++;
        }

        return chunks;
    }
}