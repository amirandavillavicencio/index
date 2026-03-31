using AppPortable.Core.Enums;
using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;

namespace AppPortable.Tests;

public sealed class ChunkingServiceTests
{
    [Fact]
    public void CreateChunks_WithRealParagraphs_ProducesCoherentChunks()
    {
        var page1Text = string.Join(
            "\n\n",
            Enumerable.Range(1, 8)
                .Select(i => $"Página uno párrafo {i} con contenido útil para búsqueda y chunking de documentos."));

        var page2Text = string.Join(
            "\n\n",
            Enumerable.Range(1, 8)
                .Select(i => $"Página dos párrafo {i} con más contenido útil para validar rangos de páginas."));

        var pages = new List<DocumentPage>
        {
            new()
            {
                PageNumber = 1,
                ExtractionLayer = ExtractionLayer.Native,
                Text = page1Text,
                TextLength = page1Text.Length
            },
            new()
            {
                PageNumber = 2,
                ExtractionLayer = ExtractionLayer.Ocr,
                Text = page2Text,
                TextLength = page2Text.Length
            }
        };

        var sut = new ParagraphChunkingService(targetChars: 420, minimumChars: 120);

        var chunks = sut.CreateChunks("doc_chunk", "sample.pdf", pages);

        Assert.NotEmpty(chunks);
        Assert.All(chunks, chunk =>
        {
            Assert.False(string.IsNullOrWhiteSpace(chunk.Text));
            Assert.True(chunk.TextLength >= 120);
            Assert.True(chunk.TextLength <= 525);
            Assert.True(chunk.PageStart >= 1);
            Assert.True(chunk.PageEnd >= chunk.PageStart);
        });

        Assert.Contains(chunks, c => c.PageStart == 1 && c.PageEnd >= 2);

        var allLayerNames = chunks
            .SelectMany(c => c.ExtractionLayersInvolved)
            .Select(layer => layer.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Assert.Contains("Native", allLayerNames, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("Ocr", allLayerNames, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public void CreateChunks_WithOnlyEmptyPages_ReturnsEmptyCollection()
    {
        var pages = new List<DocumentPage>
        {
            new()
            {
                PageNumber = 1,
                ExtractionLayer = ExtractionLayer.Failed,
                Text = "   ",
                TextLength = 0
            }
        };

        var sut = new ParagraphChunkingService();

        var chunks = sut.CreateChunks("doc_empty", "empty.pdf", pages);

        Assert.Empty(chunks);
    }
}
