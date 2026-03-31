using AppPortable.Core.Enums;

namespace AppPortable.Core.Models;

public sealed class DocumentChunk
{
    public string ChunkId { get; init; } = string.Empty;
    public string DocumentId { get; init; } = string.Empty;
    public string SourceFile { get; init; } = string.Empty;
    public int PageStart { get; init; }
    public int PageEnd { get; init; }
    public int ChunkIndex { get; init; }
    public string Text { get; init; } = string.Empty;
    public int TextLength { get; init; }
    public IReadOnlyList<ExtractionLayer> ExtractionLayersInvolved { get; init; } = [];
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();
}
