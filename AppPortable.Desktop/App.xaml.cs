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
        var tesseractDirectory = ResolvePortableTesseractDirectory();
        if (tesseractDirectory is null)
        {
            return;
        }

        var tesseractExeName = OperatingSystem.IsWindows() ? "tesseract.exe" : "tesseract";
        var tesseractExecutablePath = Path.Combine(tesseractDirectory, tesseractExeName);

        if (File.Exists(tesseractExecutablePath))
        {
            Environment.SetEnvironmentVariable("GABRIELA_TESSERACT_EXE", tesseractExecutablePath, EnvironmentVariableTarget.Process);
        }

        var tessdata = Path.Combine(tesseractDirectory, "tessdata");
        if (Directory.Exists(tessdata))
        {
            Environment.SetEnvironmentVariable("TESSDATA_PREFIX", tessdata, EnvironmentVariableTarget.Process);
        }
    }

    private static string? ResolvePortableTesseractDirectory()
    {
        var appBase = AppContext.BaseDirectory;

        string[] candidates =
        [
            Path.Combine(appBase, "tesseract"),
            Path.Combine(appBase, "tools", "tesseract")
        ];

        return candidates.FirstOrDefault(Directory.Exists);
    }
}
