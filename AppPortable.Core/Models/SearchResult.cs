namespace AppPortable.Core.Models;

public sealed class SearchResult
{
    public string ChunkId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string SourceFile { get; set; } = string.Empty;
    public int PageStart { get; set; }
    public int PageEnd { get; set; }
    public double Score { get; set; }
    public string Snippet { get; set; } = string.Empty;
    public string? ChunkText { get; set; }
}
