using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Services;

public sealed class ParagraphChunkingService : IChunkingService
{
    private const double MaxChunkOverflowFactor = 1.25d;

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

        var maxChunkChars = Math.Max(_targetChars, (int)Math.Round(_targetChars * MaxChunkOverflowFactor));
        var segments = BuildSegments(pages);
        if (segments.Count == 0)
            return Array.Empty<DocumentChunk>();

        var batches = BuildBatches(segments, maxChunkChars);
        MergeFinalSmallBatch(batches);

        var chunks = new List<DocumentChunk>(batches.Count);
        for (var i = 0; i < batches.Count; i++)
        {
            var chunkText = string.Join("\n\n", batches[i].Select(s => s.Text));
            var layers = batches[i]
                .Select(s => s.Layer)
                .Distinct()
                .ToArray();

            chunks.Add(new DocumentChunk
            {
                ChunkId = $"{documentId}-{i:D4}",
                DocumentId = documentId,
                SourceFile = sourceFile,
                PageStart = batches[i][0].PageNumber,
                PageEnd = batches[i][^1].PageNumber,
                ChunkIndex = i,
                Text = chunkText,
                TextLength = chunkText.Length,
                ExtractionLayersInvolved = layers,
                Metadata = new Dictionary<string, string>
                {
                    ["strategy"] = "paragraph_aware",
                    ["target_chars"] = _targetChars.ToString(),
                    ["minimum_chars"] = _minimumChars.ToString()
                }
            });
        }

        return chunks;
    }

    private List<List<ChunkSegment>> BuildBatches(List<ChunkSegment> segments, int maxChunkChars)
    {
        var batches = new List<List<ChunkSegment>>();
        var currentBatch = new List<ChunkSegment>();
        var currentLength = 0;

        foreach (var segment in segments)
        {
            var separatorLength = currentBatch.Count == 0 ? 0 : 2;
            var projectedLength = currentLength + separatorLength + segment.Text.Length;

            if (currentBatch.Count > 0 && projectedLength > maxChunkChars)
            {
                batches.Add(currentBatch);
                currentBatch = new List<ChunkSegment>();
                currentLength = 0;
                separatorLength = 0;
            }

            currentBatch.Add(segment);
            currentLength += separatorLength + segment.Text.Length;
        }

        if (currentBatch.Count > 0)
            batches.Add(currentBatch);

        return batches;
    }

    private void MergeFinalSmallBatch(List<List<ChunkSegment>> batches)
    {
        if (batches.Count < 2)
            return;

        var lastBatchLength = GetBatchLength(batches[^1]);
        if (lastBatchLength >= _minimumChars)
            return;

        batches[^2].AddRange(batches[^1]);
        batches.RemoveAt(batches.Count - 1);
    }

    private static int GetBatchLength(IReadOnlyCollection<ChunkSegment> batch)
    {
        var paragraphChars = batch.Sum(s => s.Text.Length);
        var separators = Math.Max(0, batch.Count - 1) * 2;
        return paragraphChars + separators;
    }

    private static List<ChunkSegment> BuildSegments(IReadOnlyList<DocumentPage> pages)
    {
        var segments = new List<ChunkSegment>();

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
                continue;

            var paragraphs = page.Text
                .Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.None)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p));

            foreach (var paragraph in paragraphs)
            {
                segments.Add(new ChunkSegment(page.PageNumber, page.ExtractionLayer, paragraph));
            }
        }

        return segments;
    }

    private sealed record ChunkSegment(int PageNumber, Core.Enums.ExtractionLayer Layer, string Text);
}
