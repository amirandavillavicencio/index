using System;
using System.IO;
using System.Linq;
using System.Windows;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;

namespace AppPortable.Desktop;

public partial class App : Application
{
    private const string PortableDataFolderName = "data";
    private const string LocalDataFolderName = "Gabriela";

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigurePortableRuntimePaths();

        var basePath = ResolveStoragePath();

        var localStorageService = new LocalStorageService(basePath);
        var pdfExtractionService = new PdfExtractionService();
        var ocrService = new TesseractOcrService();
        var chunkingService = new ParagraphChunkingService();
        var jsonPersistenceService = new JsonPersistenceService();
        var indexService = new SqliteIndexService(localStorageService);
        var searchService = new SqliteSearchService(localStorageService, indexService);

        await indexService.EnsureInitializedAsync();

        var processor = new InfrastructureDocumentProcessor(
            localStorageService,
            pdfExtractionService,
            ocrService,
            chunkingService,
            jsonPersistenceService,
            indexService);

        var mainWindow = new MainWindow(processor, searchService);
        mainWindow.Show();
    }

    private static string ResolveStoragePath()
    {
        var appBase = AppContext.BaseDirectory;
        var portableDataPath = Path.Combine(appBase, PortableDataFolderName);

        if (CanUsePortableStorage(portableDataPath))
        {
            return portableDataPath;
        }

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(localAppData, LocalDataFolderName);
    }

    private static bool CanUsePortableStorage(string portableDataPath)
    {
        try
        {
            Directory.CreateDirectory(portableDataPath);
            var testPath = Path.Combine(portableDataPath, ".write-test");
            File.WriteAllText(testPath, "ok");
            File.Delete(testPath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static void ConfigurePortableRuntimePaths()
    {
        var appBase = AppContext.BaseDirectory;
        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        string[] candidates =
        [
            Path.Combine(appBase, "tesseract"),
            Path.Combine(appBase, "tools", "tesseract"),
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Tesseract-OCR"),
            @"C:\Program Files\Tesseract-OCR",
            @"C:\Program Files (x86)\Tesseract-OCR"
        ];

        var resolved = candidates.Where(Directory.Exists).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (resolved.Count == 0)
        {
            return;
        }

        foreach (var dir in resolved)
        {
            if (!PathContains(currentPath, dir))
            {
                currentPath = string.IsNullOrWhiteSpace(currentPath)
                    ? dir
                    : $"{dir}{Path.PathSeparator}{currentPath}";
            }

            var tessdata = Path.Combine(dir, "tessdata");
            if (Directory.Exists(tessdata))
            {
                Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tessdata, EnvironmentVariableTarget.Process);
                break;
            }
        }

        Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.Process);
    }

    private static bool PathContains(string pathValue, string directory)
    {
        return pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => string.Equals(entry.Trim(), directory, StringComparison.OrdinalIgnoreCase));
    }
}
