using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Search.Services;

public sealed class SqliteSearchService(ILocalStorageService localStorageService, IIndexService indexService) : ISearchService
{
    public async Task<IReadOnlyList<SearchResult>> SearchAsync(string query, int limit = 50, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return [];
        }

        await indexService.EnsureInitializedAsync(cancellationToken);
        await using var connection = new SqliteConnection($"Data Source={localStorageService.DatabasePath}");
        await connection.OpenAsync(cancellationToken);

        const string sql = """
                           SELECT c.chunk_id,
                                  c.document_id,
                                  c.source_file,
                                  c.page_start,
                                  c.page_end,
                                  bm25(document_chunks_fts) AS score,
                                  snippet(document_chunks_fts, 3, '[', ']', ' … ', 24) AS snippet,
                                  c.text
                           FROM document_chunks_fts
                           JOIN document_chunks c ON c.rowid = document_chunks_fts.rowid
                           WHERE document_chunks_fts MATCH $query
                           ORDER BY score
                           LIMIT $limit;
                           """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.Parameters.AddWithValue("$query", query.Trim());
        command.Parameters.AddWithValue("$limit", limit);

        var results = new List<SearchResult>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            results.Add(new SearchResult
            {
                ChunkId = reader.GetString(0),
                DocumentId = reader.GetString(1),
                SourceFile = reader.GetString(2),
                PageStart = reader.GetInt32(3),
                PageEnd = reader.GetInt32(4),
                Score = reader.GetDouble(5),
                Snippet = reader.IsDBNull(6) ? string.Empty : reader.GetString(6),
                ChunkText = reader.IsDBNull(7) ? null : reader.GetString(7)
            });
        }

        return results;
    }
}
