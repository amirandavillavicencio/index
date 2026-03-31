using AppPortable.Core.Enums;

namespace AppPortable.Core.Models;

public sealed class DocumentPage
{
    public int PageNumber { get; init; }
    public ExtractionLayer ExtractionLayer { get; init; }
    public double? OcrConfidence { get; init; }
    public string Text { get; init; } = string.Empty;
    public int TextLength { get; init; }
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
