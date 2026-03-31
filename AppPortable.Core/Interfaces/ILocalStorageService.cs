namespace AppPortable.Core.Interfaces;

public interface ILocalStorageService
{
    string RootPath { get; }
    string DocumentsPath { get; }
    string JsonPath { get; }
    string ChunksPath { get; }
    string IndexPath { get; }
    string TempPath { get; }
    string LogsPath { get; }
    string DatabasePath { get; }

    void EnsureInitialized();
    Task<string> CopySourceDocumentAsync(string sourcePdfPath, CancellationToken cancellationToken = default);
    string GetDocumentJsonPath(string documentId);
    string GetChunksJsonPath(string documentId);
}
