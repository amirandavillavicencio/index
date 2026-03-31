using System.IO;
using AppPortable.Core.Models;

namespace AppPortable.Desktop.Models;

public sealed class DocumentListItem
{
    public required ProcessedDocument Document { get; init; }
    public string Title => $"{Document.DocumentId} ({Path.GetFileName(Document.SourceFile)})";
}
