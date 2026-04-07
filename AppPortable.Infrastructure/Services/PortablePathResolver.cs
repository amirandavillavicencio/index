namespace AppPortable.Infrastructure.Services;

public static class PortablePathResolver
{
    public const string PortableDataFolderName = "data";
    public const string LocalDataFolderName = "Gabriela";

    public static string ResolveStorageRootPath()
    {
        var portableDataPath = Path.Combine(AppContext.BaseDirectory, PortableDataFolderName);
        if (CanWriteToDirectory(portableDataPath))
        {
            return portableDataPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, LocalDataFolderName);
    }

    public static string GetPortableTesseractDirectory()
        => Path.Combine(AppContext.BaseDirectory, "tesseract");

    public static string GetPortableTesseractExecutablePath()
    {
        var exeName = OperatingSystem.IsWindows() ? "tesseract.exe" : "tesseract";
        return Path.Combine(GetPortableTesseractDirectory(), exeName);
    }

    public static string GetPortableTessdataDirectory()
        => Path.Combine(GetPortableTesseractDirectory(), "tessdata");

    public static string GetPortableSpanishLanguageModelPath()
        => Path.Combine(GetPortableTessdataDirectory(), "spa.traineddata");

    public static string? ResolveTesseractExecutablePathPortableFirst()
    {
        var portableExePath = GetPortableTesseractExecutablePath();
        if (File.Exists(portableExePath))
        {
            return portableExePath;
        }

        var exeName = OperatingSystem.IsWindows() ? "tesseract.exe" : "tesseract";
        var pathEnv = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            var full = Path.Combine(dir.Trim(), exeName);
            if (File.Exists(full))
            {
                return full;
            }
        }

        return null;
    }

    public static bool CanWriteToDirectory(string targetDirectory)
    {
        try
        {
            Directory.CreateDirectory(targetDirectory);
            var testPath = Path.Combine(targetDirectory, $".write-test-{Guid.NewGuid():N}");
            File.WriteAllText(testPath, "ok");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            return false;
        }
    }
}
