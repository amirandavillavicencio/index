namespace AppPortable.Core.Models;

public sealed class ProcessedDocument
{
    public string DocumentId { get; init; } = string.Empty;
    public string SourceFile { get; init; } = string.Empty;
    public DateTime ProcessedAt { get; init; }
    public int TotalPages { get; init; }
    public ExtractionSummary ExtractionSummary { get; init; } = new();
    public IReadOnlyList<DocumentPage> Pages { get; init; } = [];
    public IReadOnlyList<DocumentChunk> Chunks { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
