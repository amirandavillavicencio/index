using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;

namespace AppPortable.Tests;

public sealed class SearchIndexTests
{
    [Fact]
    public async Task IndexAndSearchAsync_ReturnsMatchingChunk()
    {
        var root = Path.Combine(Path.GetTempPath(), $"appportable_{Guid.NewGuid():N}");
        var storage = new LocalStorageService(root);
        var index = new SqliteIndexService(storage);
        var search = new SqliteSearchService(storage, index);

        var doc = new ProcessedDocument
        {
            DocumentId = "doc1",
            SourceFile = "sample.pdf",
            Chunks =
            [
                new DocumentChunk
                {
                    ChunkId = "doc1-0000",
                    DocumentId = "doc1",
                    SourceFile = "sample.pdf",
                    PageStart = 1,
                    PageEnd = 1,
                    ChunkIndex = 0,
                    Text = "arquitectura limpia de procesamiento documental",
                    TextLength = 45
                }
            ]
        };

        await index.IndexDocumentAsync(doc);
        var results = await search.SearchAsync("arquitectura");

        Assert.Single(results);
        Assert.Equal("doc1-0000", results[0].ChunkId);
    }
}
