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

        await ExecAsync(connection, null, """
            CREATE TABLE IF NOT EXISTS document_chunks (
                chunk_id   TEXT PRIMARY KEY,
                document_id TEXT NOT NULL,
                source_file TEXT NOT NULL,
                page_start  INTEGER NOT NULL,
                page_end    INTEGER NOT NULL,
                chunk_index INTEGER NOT NULL,
                text        TEXT NOT NULL
            );
            """, cancellationToken);

        await ExecAsync(connection, null, """
            CREATE INDEX IF NOT EXISTS idx_document_chunks_document_id
            ON document_chunks(document_id);
            """, cancellationToken);

        await ExecAsync(connection, null, """
            CREATE VIRTUAL TABLE IF NOT EXISTS document_chunks_fts USING fts5(
                chunk_id    UNINDEXED,
                document_id UNINDEXED,
                source_file UNINDEXED,
                text,
                tokenize = 'unicode61 remove_diacritics 2'
            );
            """, cancellationToken);
    }

    public async Task IndexChunksAsync(string documentId, IReadOnlyList<DocumentChunk> chunks, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await DeleteDocumentChunksAsync(connection, tx, documentId, cancellationToken);

        foreach (var chunk in chunks)
            await InsertChunkAsync(connection, tx, chunk, cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    public Task IndexDocumentAsync(ProcessedDocument document, CancellationToken cancellationToken = default)
        => IndexChunksAsync(document.DocumentId, document.Chunks, cancellationToken);

    public async Task RebuildIndexAsync(IEnumerable<ProcessedDocument> documents, CancellationToken cancellationToken = default)
    {
        await EnsureInitializedAsync(cancellationToken);

        await using var connection = OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var tx = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);

        await ExecAsync(connection, tx, "DELETE FROM document_chunks_fts;", cancellationToken);
        await ExecAsync(connection, tx, "DELETE FROM document_chunks;",     cancellationToken);

        foreach (var document in documents)
            foreach (var chunk in document.Chunks)
                await InsertChunkAsync(connection, tx, chunk, cancellationToken);

        await tx.CommitAsync(cancellationToken);
    }

    private static async Task DeleteDocumentChunksAsync(
        SqliteConnection connection, SqliteTransaction tx,
        string documentId, CancellationToken ct)
    {
        // 1) Borrar de FTS
        await using var cmd1 = Cmd(connection, tx,
            "DELETE FROM document_chunks_fts WHERE rowid IN (SELECT rowid FROM document_chunks WHERE document_id = $id);");
        cmd1.Parameters.AddWithValue("$id", documentId);
        await cmd1.ExecuteNonQueryAsync(ct);

        // 2) Borrar de tabla relacional
        await using var cmd2 = Cmd(connection, tx,
            "DELETE FROM document_chunks WHERE document_id = $id;");
        cmd2.Parameters.AddWithValue("$id", documentId);
        await cmd2.ExecuteNonQueryAsync(ct);
    }

    private static async Task InsertChunkAsync(
        SqliteConnection connection, SqliteTransaction tx,
        DocumentChunk chunk, CancellationToken ct)
    {
        // 1) Upsert relacional
        await using var cmd1 = Cmd(connection, tx, """
            INSERT INTO document_chunks (chunk_id, document_id, source_file, page_start, page_end, chunk_index, text)
            VALUES ($chunk_id, $document_id, $source_file, $page_start, $page_end, $chunk_index, $text)
            ON CONFLICT(chunk_id) DO UPDATE SET
                document_id = excluded.document_id,
                source_file = excluded.source_file,
                page_start  = excluded.page_start,
                page_end    = excluded.page_end,
                chunk_index = excluded.chunk_index,
                text        = excluded.text;
            """);
        AddChunkParams(cmd1, chunk);
        await cmd1.ExecuteNonQueryAsync(ct);

        // 2) Limpiar FTS si ya existía (re-indexación)
        await using var cmd2 = Cmd(connection, tx,
            "DELETE FROM document_chunks_fts WHERE rowid = (SELECT rowid FROM document_chunks WHERE chunk_id = $chunk_id);");
        cmd2.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
        await cmd2.ExecuteNonQueryAsync(ct);

        // 3) Insertar en FTS5
        await using var cmd3 = Cmd(connection, tx, """
            INSERT INTO document_chunks_fts(rowid, chunk_id, document_id, source_file, text)
            SELECT rowid, chunk_id, document_id, source_file, text
            FROM document_chunks
            WHERE chunk_id = $chunk_id;
            """);
        cmd3.Parameters.AddWithValue("$chunk_id", chunk.ChunkId);
        await cmd3.ExecuteNonQueryAsync(ct);
    }

    private static void AddChunkParams(SqliteCommand cmd, DocumentChunk chunk)
    {
        cmd.Parameters.AddWithValue("$chunk_id",    chunk.ChunkId);
        cmd.Parameters.AddWithValue("$document_id", chunk.DocumentId);
        cmd.Parameters.AddWithValue("$source_file", chunk.SourceFile);
        cmd.Parameters.AddWithValue("$page_start",  chunk.PageStart);
        cmd.Parameters.AddWithValue("$page_end",    chunk.PageEnd);
        cmd.Parameters.AddWithValue("$chunk_index", chunk.ChunkIndex);
        cmd.Parameters.AddWithValue("$text",        chunk.Text);
    }

    private static SqliteCommand Cmd(SqliteConnection conn, SqliteTransaction? tx, string sql)
    {
        var cmd = conn.CreateCommand();
        cmd.Transaction = tx;
        cmd.CommandText = sql;
        return cmd;
    }

    private static async Task ExecAsync(
        SqliteConnection conn, SqliteTransaction? tx, string sql, CancellationToken ct = default)
    {
        await using var cmd = Cmd(conn, tx, sql);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    private SqliteConnection OpenConnection() =>
        new($"Data Source={localStorageService.DatabasePath}");
}

