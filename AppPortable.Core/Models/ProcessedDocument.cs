namespace AppPortable.Core.Models;

public sealed class ProcessedDocument
{
    public string DocumentId { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; set; }
    public int TotalPages { get; set; }
    public ExtractionSummary ExtractionSummary { get; set; } = new();
    public List<DocumentPage> Pages { get; set; } = [];
    public List<DocumentChunk> Chunks { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}
