using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using Microsoft.Data.Sqlite;

namespace AppPortable.Search.Services;

public sealed class SqliteIndexService(ILocalStorageService localStorageService) : IIndexService
{
    public async Task EnsureInitializedAsync(CancellationToken cancellationToken = default)
    {
        localStorageService.EnsureInitialized();
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var sql = """
                  CREATE TABLE IF NOT EXISTS document_chunks (
                      chunk_id TEXT PRIMARY KEY,
                      document_id TEXT NOT NULL,
                      source_file TEXT NOT NULL,
                      page_start INTEGER NOT NULL,
                      page_end INTEGER NOT NULL,
                      chunk_index INTEGER NOT NULL,
                      text TEXT NOT NULL
                  );

                  CREATE VIRTUAL TABLE IF NOT EXISTS document_chunks_fts USING fts5(
                      chunk_id UNINDEXED,
                      document_id UNINDEXED,
                      source_file UNINDEXED,
                      text,
                      content='document_chunks',
                      content_rowid='rowid'
                  );
                  """;

        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var chunk in document.Chunks)
        {
            await UpsertChunkAsync(connection, transaction, chunk, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
    }

    public async Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);
        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);

        var resetSql = "DELETE FROM document_chunks; DELETE FROM document_chunks_fts;";
        await using (var resetCmd = connection.CreateCommand())
        {
            resetCmd.CommandText = resetSql;
            await resetCmd.ExecuteNonQueryAsync(cancellationToken);
        }

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var document in documents)
        {
            foreach (var chunk in document.Chunks)
            {
                await UpsertChunkAsync(connection, transaction, chunk, cancellationToken);
            }
        }

        await transaction.CommitAsync(cancellationToken);
    }

    private async Task UpsertChunkAsync(SqliteConnection connection, SqliteTransaction transaction, DocumentChunk chunk, CancellationToken cancellationToken)
    {
        const string sql = """
                           INSERT INTO document_chunks (chunk_id, document_id, source_file, page_start, page_end, chunk_index, text)
                           VALUES ($chunk_id, $document_id, $source_file, $page_start, $page_end, $chunk_index, $text)
                           ON CONFLICT(chunk_id) DO UPDATE SET
                               document_id = excluded.document_id,
                               source_file = excluded.source_file,
                               page_start = excluded.page_start,
                               page_end = excluded.page_end,
                               chunk_index = excluded.chunk_index,
                               text = excluded.text;

                           INSERT INTO document_chunks_fts(rowid, chunk_id, document_id, source_file, text)
                           VALUES ((SELECT rowid FROM document_chunks WHERE chunk_id = $chunk_id), $chunk_id, $document_id, $source_file, $text)
                           ON CONFLICT(rowid) DO UPDATE SET
                               chunk_id = excluded.chunk_id,
                               document_id = excluded.document_id,
                               source_file = excluded.source_file,
                               text = excluded.text;
                           """;

        await using var command = connection.CreateCommand();
        command.Transaction = transaction;
        command.CommandText = sql;
        command.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
        command.Parameters.AddWithValue("$document_id", chunk.DocumentId);
        command.Parameters.AddWithValue("$source_file", chunk.SourceFile);
        command.Parameters.AddWithValue("$page_start", chunk.PageStart);
        command.Parameters.AddWithValue("$page_end", chunk.PageEnd);
        command.Parameters.AddWithValue("$chunk_index", chunk.ChunkIndex);
        command.Parameters.AddWithValue("$text", chunk.Text);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private SqliteConnection OpenConnection() => new($"Data Source={localStorageService.DatabasePath}");
}
