using AppPortable.Core.Enums;
using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;

namespace AppPortable.Tests;

public sealed class ChunkingServiceTests
{
    [Fact]
    public void CreateChunks_GeneratesChunksWithPageRanges()
    {
        var pages = new List<DocumentPage>
        {
            new()
            {
                PageNumber = 1,
                ExtractionLayer = ExtractionLayer.Native,
                Text = string.Join("\n\n", Enumerable.Range(1, 8).Select(i => $"Párrafo uno oración {i}. Texto adicional para tamaño.")),
                TextLength = 600
            },
            new()
            {
                PageNumber = 2,
                ExtractionLayer = ExtractionLayer.Native,
                Text = string.Join("\n\n", Enumerable.Range(1, 8).Select(i => $"Párrafo dos oración {i}. Texto adicional para tamaño.")),
                TextLength = 600
            }
        };

        var sut = new ParagraphChunkingService(450, 100);
        var chunks = sut.CreateChunks("doc1", "sample.pdf", pages);

        Assert.True(chunks.Count >= 2);
        Assert.All(chunks, c => Assert.True(c.TextLength >= 100));
        Assert.Contains(chunks, c => c.PageStart == 1 && c.PageEnd >= 1);
    }
}
