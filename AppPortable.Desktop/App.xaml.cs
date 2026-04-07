using System.Windows;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;

namespace AppPortable.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        ConfigurePortableRuntimePaths();

        var basePath = PortablePathResolver.ResolveStorageRootPath();

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

    private static void ConfigurePortableRuntimePaths()
    {
        var portableTesseractDir = PortablePathResolver.GetPortableTesseractDirectory();
        if (!Directory.Exists(portableTesseractDir))
        {
            return;
        }

        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
        if (!PathContains(currentPath, portableTesseractDir))
        {
            currentPath = string.IsNullOrWhiteSpace(currentPath)
                ? portableTesseractDir
                : $"{portableTesseractDir}{Path.PathSeparator}{currentPath}";

            Environment.SetEnvironmentVariable("PATH", currentPath, EnvironmentVariableTarget.Process);
        }

        var portableTessdata = PortablePathResolver.GetPortableTessdataDirectory();
        if (Directory.Exists(portableTessdata))
        {
            Environment.SetEnvironmentVariable("TESSDATA_PREFIX", portableTessdata, EnvironmentVariableTarget.Process);
        }
    }

    private static bool PathContains(string pathValue, string directory)
    {
        return pathValue
            .Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries)
            .Any(entry => string.Equals(entry.Trim(), directory, StringComparison.OrdinalIgnoreCase));
    }
}
