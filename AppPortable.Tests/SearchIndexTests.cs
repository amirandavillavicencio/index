using AppPortable.Core.Enums;
using AppPortable.Core.Models;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;
using AppPortable.Tests.TestHelpers;
using Xunit;

namespace AppPortable.Tests;

public sealed class SearchIndexTests
{
    [Fact]
    public async Task EnsureInitializeIndexAndSearch_ReturnsUsefulResultsWithSnippetAndScore()
    {
        using var tempDir = new TemporaryDirectory("search_index");
        var storage = new LocalStorageService(tempDir.Path);
        var index = new SqliteIndexService(storage);
        var search = new SqliteSearchService(storage, index);

        await index.EnsureInitializedAsync();

        Assert.True(File.Exists(storage.DatabasePath));

        var doc = BuildDocument(
            "doc-search-1",
            "manual.pdf",
            "La arquitectura limpia permite desacoplar módulos y optimiza la mantenibilidad.",
            "La búsqueda full text en sqlite fts5 permite localizar términos relevantes rápidamente.");

        await index.IndexDocumentAsync(doc);

        var results = await search.SearchAsync("sqlite");

        Assert.NotEmpty(results);
        var first = results[0];
        Assert.Equal("doc-search-1", first.DocumentId);
        Assert.False(double.IsNaN(first.Score));
        Assert.NotEmpty(first.Snippet);
        Assert.Contains("sqlite", first.Snippet, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task RebuildIndexAsync_ReplacesPreviousStateAndKeepsConsistency()
    {
        using var tempDir = new TemporaryDirectory("search_rebuild");
        var storage = new LocalStorageService(tempDir.Path);
        var index = new SqliteIndexService(storage);
        var search = new SqliteSearchService(storage, index);

        var oldDoc = BuildDocument("doc-old", "old.pdf", "contenido legado", "nube privada");
        await index.IndexDocumentAsync(oldDoc);

        var newDoc = BuildDocument("doc-new", "new.pdf", "orquestación kubernetes", "observabilidad y métricas");
        await index.RebuildIndexAsync([newDoc]);

        var oldResults = await search.SearchAsync("legado");
        var newResults = await search.SearchAsync("kubernetes");

        Assert.Empty(oldResults);
        Assert.Single(newResults);
        Assert.Equal("doc-new", newResults[0].DocumentId);
    }

    private static ProcessedDocument BuildDocument(string documentId, string sourceFile, params string[] chunkTexts)
    {
        var chunks = chunkTexts
            .Select((text, index) => new DocumentChunk
            {
                ChunkId = $"{documentId}-{index:D4}",
                DocumentId = documentId,
                SourceFile = sourceFile,
                PageStart = 1,
                PageEnd = 1,
                ChunkIndex = index,
                Text = text,
                TextLength = text.Length,
                ExtractionLayersInvolved = [ExtractionLayer.Native]
            })
            .ToArray();

        return new ProcessedDocument
        {
            DocumentId = documentId,
            SourceFile = sourceFile,
            TotalPages = 1,
            Chunks = chunks
        };
    }
}
