using AppPortable.Core.Enums;

namespace AppPortable.Core.Models;

public sealed class DocumentPage
{
    public int PageNumber { get; set; }
    public ExtractionLayer ExtractionLayer { get; set; }
    public double? OcrConfidence { get; set; }
    public string Text { get; set; } = string.Empty;
    public int TextLength { get; set; }
    public List<string> Warnings { get; set; } = [];
}
