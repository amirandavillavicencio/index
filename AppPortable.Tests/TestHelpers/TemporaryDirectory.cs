namespace AppPortable.Tests.TestHelpers;

internal sealed class TemporaryDirectory : IDisposable
{
    public string Path { get; }

    public TemporaryDirectory(string? prefix = null)
    {
        var folderName = $"{prefix ?? "appportable_tests"}_{Guid.NewGuid():N}";
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), folderName);
        Directory.CreateDirectory(Path);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path))
            {
                Directory.Delete(Path, recursive: true);
            }
        }
        catch
        {
            // No-op in tests cleanup.
        }
    }
}
