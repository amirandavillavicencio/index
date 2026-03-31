// ============================================================
// AppPortable.Infrastructure/Services/ParagraphChunkingService.cs
// ============================================================

using System.Text;
using System.Text.RegularExpressions;
using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Services;

public sealed class ParagraphChunkingService : IChunkingService
{
    private readonly int _targetChars;
    private readonly int _minimumChars;

    public ParagraphChunkingService(int targetChars = 1200, int minimumChars = 180)
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

        var paragraphItems = SplitParagraphs(pages);
        if (paragraphItems.Count == 0)
        {
            return [];
        }

        var chunks = new List<DocumentChunk>();
        var overlapChars = (int)Math.Round(_targetChars * 0.1);
        var chunkIndex = 0;
        var cursor = 0;

        while (cursor < paragraphItems.Count)
        {
            var selected = new List<ParagraphItem>();
            var charCount = 0;

            while (cursor < paragraphItems.Count && (charCount < _targetChars || selected.Count == 0))
            {
                var candidate = paragraphItems[cursor];
                if (selected.Count > 0 && charCount + candidate.Text.Length > _targetChars * 1.25)
                {
                    break;
                }

                selected.Add(candidate);
                charCount += candidate.Text.Length;
                cursor++;
            }

            var chunkText = JoinParagraphs(selected.Select(s => s.Text));
            if (chunkText.Length >= _minimumChars)
            {
                chunks.Add(BuildChunk(documentId, sourceFile, chunkIndex, chunkText, selected));
                chunkIndex++;
            }

            if (cursor >= paragraphItems.Count)
            {
                break;
            }

            var backtrackChars = 0;
            var backtrackCount = 0;

            for (var i = selected.Count - 1; i >= 0; i--)
            {
                backtrackChars += selected[i].Text.Length;
                backtrackCount++;

                if (backtrackChars >= overlapChars)
                {
                    break;
                }
            }

            cursor = Math.Max(0, cursor - backtrackCount);

            if (backtrackCount == 0)
            {
                cursor++;
            }
        }

        return chunks;
    }

    private static List<ParagraphItem> SplitParagraphs(IReadOnlyList<DocumentPage> pages)
    {
        var list = new List<ParagraphItem>();

        foreach (var page in pages)
        {
            if (string.IsNullOrWhiteSpace(page.Text))
            {
                continue;
            }

            var normalized = page.Text.Replace("\r\n", "\n");
            var paragraphs = Regex.Split(normalized, "\\n{2,}")
                .Select(p => p.Trim())
                .Where(p => p.Length > 0)
                .Select(NormalizeParagraph);

            foreach (var paragraph in paragraphs)
            {
                list.Add(new ParagraphItem(paragraph, page.PageNumber, page.ExtractionLayer));
            }
        }

        return list;
    }

    private static string NormalizeParagraph(string paragraph)
    {
        var compact = Regex.Replace(paragraph, "\\s+", " ").Trim();

        if (compact.Length == 0)
        {
            return string.Empty;
        }

        if (compact.EndsWith('.') || compact.EndsWith('!') || compact.EndsWith('?'))
        {
            return compact;
        }

        return compact + ".";
    }

    private static string JoinParagraphs(IEnumerable<string> paragraphs)
    {
        var sb = new StringBuilder();

        foreach (var paragraph in paragraphs)
        {
            if (string.IsNullOrWhiteSpace(paragraph))
            {
                continue;
            }

            if (sb.Length > 0)
            {
                sb.AppendLine();
                sb.AppendLine();
            }

            sb.Append(paragraph);
        }

        return sb.ToString();
    }

    private static DocumentChunk BuildChunk(
        string documentId,
        string sourceFile,
        int chunkIndex,
        string chunkText,
        IReadOnlyList<ParagraphItem> selected)
    {
        var pageStart = selected.Min(s => s.PageNumber);
        var pageEnd = selected.Max(s => s.PageNumber);
        var layers = selected.Select(s => s.Layer).Distinct().ToList();

        return new DocumentChunk
        {
            ChunkId = $"{documentId}-{chunkIndex:D4}",
            DocumentId = documentId,
            SourceFile = sourceFile,
            ChunkIndex = chunkIndex,
            PageStart = pageStart,
            PageEnd = pageEnd,
            Text = chunkText,
            TextLength = chunkText.Length,
            ExtractionLayersInvolved = layers,
            Metadata = new Dictionary<string, string>
            {
                ["paragraph_count"] = selected.Count.ToString(),
                ["strategy"] = "paragraph_overlap_10pct"
            }
        };
    }

    private sealed record ParagraphItem(string Text, int PageNumber, ExtractionLayer Layer);
}