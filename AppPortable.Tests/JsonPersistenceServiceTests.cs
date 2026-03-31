using AppPortable.Core.Enums;
using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;
using AppPortable.Tests.TestHelpers;

namespace AppPortable.Tests;

public sealed class JsonPersistenceServiceTests
{
    [Fact]
    public async Task SaveAndLoadDocumentAndChunks_RoundTripsWithoutCorruptionArtifacts()
    {
        using var tempDir = new TemporaryDirectory("json_roundtrip");
        var documentPath = Path.Combine(tempDir.Path, "docs", "processed.json");
        var chunksPath = Path.Combine(tempDir.Path, "chunks", "processed.chunks.json");
        var sut = new JsonPersistenceService();

        var chunk = new DocumentChunk
        {
            ChunkId = "doc-json-0000",
            DocumentId = "doc-json",
            SourceFile = "sample.pdf",
            ChunkIndex = 0,
            PageStart = 1,
            PageEnd = 1,
            Text = "texto persistido para verificar guardado y carga",
            TextLength = 47,
            ExtractionLayersInvolved = [ExtractionLayer.Native],
            Metadata = new Dictionary<string, string> { ["strategy"] = "unit_test" }
        };

        var input = new ProcessedDocument
        {
            DocumentId = "doc-json",
            SourceFile = "sample.pdf",
            ProcessedAt = new DateTime(2026, 3, 31, 12, 0, 0, DateTimeKind.Utc),
            TotalPages = 1,
            Pages =
            [
                new DocumentPage
                {
                    PageNumber = 1,
                    ExtractionLayer = ExtractionLayer.Native,
                    Text = "texto página",
                    TextLength = 11
                }
            ],
            Chunks = [chunk]
        };

        await sut.SaveDocumentAsync(documentPath, input);
        await sut.SaveChunksAsync(chunksPath, input.Chunks);

        var loadedDocument = await sut.LoadDocumentAsync(documentPath);
        var loadedChunks = await sut.LoadChunksAsync(chunksPath);

        Assert.NotNull(loadedDocument);
        Assert.Equal(input.DocumentId, loadedDocument!.DocumentId);
        Assert.Equal(input.SourceFile, loadedDocument.SourceFile);
        Assert.Single(loadedChunks);
        Assert.Equal(chunk.ChunkId, loadedChunks[0].ChunkId);
        Assert.Equal(chunk.Text, loadedChunks[0].Text);

        var allFiles = Directory.EnumerateFiles(tempDir.Path, "*", SearchOption.AllDirectories).ToArray();
        Assert.DoesNotContain(allFiles, file => file.EndsWith(".bak", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(allFiles, file => file.Contains(".tmp", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task SaveAsync_WhenOverwritingExistingFile_KeepsReadableJson()
    {
        using var tempDir = new TemporaryDirectory("json_overwrite");
        var filePath = Path.Combine(tempDir.Path, "state.json");
        var sut = new JsonPersistenceService();

        await sut.SaveDocumentAsync(filePath, new ProcessedDocument
        {
            DocumentId = "first",
            SourceFile = "a.pdf",
            TotalPages = 1
        });

        await sut.SaveDocumentAsync(filePath, new ProcessedDocument
        {
            DocumentId = "second",
            SourceFile = "b.pdf",
            TotalPages = 2
        });

        var loaded = await sut.LoadDocumentAsync(filePath);

        Assert.NotNull(loaded);
        Assert.Equal("second", loaded!.DocumentId);
        Assert.Equal("b.pdf", loaded.SourceFile);
        Assert.Equal(2, loaded.TotalPages);
    }
}
