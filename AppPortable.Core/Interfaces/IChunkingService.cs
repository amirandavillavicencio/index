using AppPortable.Core.Models;

namespace AppPortable.Core.Interfaces;

public interface IChunkingService
{
    IReadOnlyList<DocumentChunk> CreateChunks(string documentId, string sourceFile, IReadOnlyList<DocumentPage> pages);
}
