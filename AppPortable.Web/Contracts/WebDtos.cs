using AppPortable.Core.Models;

namespace AppPortable.Web.Contracts;

public sealed record ProcessDocumentResponse(
    string DocumentId,
    string SourceFile,
    int TotalPages,
    int TotalChunks,
    ExtractionSummary ExtractionSummary,
    IReadOnlyList<string> Warnings);

public sealed record SearchRequest(string Query, int Limit = 20);

public sealed record SearchResponse(IReadOnlyList<SearchResult> Results);
