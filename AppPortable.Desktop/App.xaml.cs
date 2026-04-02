using System;
using System.IO;
using System.Windows;
using AppPortable.Infrastructure.Services;
using AppPortable.Search.Services;

namespace AppPortable.Desktop;

public partial class App : Application
{
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        EnsureTesseractInPath();

        var basePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AppPortable");

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

    private static void EnsureTesseractInPath()
    {
        string[] knownPaths =
        [
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Programs",
                "Tesseract-OCR"),
            @"C:\Program Files\Tesseract-OCR",
            @"C:\Program Files (x86)\Tesseract-OCR"
        ];

        var currentPath = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;

        foreach (var dir in knownPaths)
        {
            if (!Directory.Exists(dir))
                continue;

            if (!currentPath.Contains(dir, StringComparison.OrdinalIgnoreCase))
            {
                Environment.SetEnvironmentVariable(
                    "PATH",
                    $"{currentPath};{dir}",
                    EnvironmentVariableTarget.Process);
            }

            var tessdata = Path.Combine(dir, "tessdata");
            if (Directory.Exists(tessdata))
            {
                Environment.SetEnvironmentVariable(
                    "TESSDATA_PREFIX",
                    tessdata,
                    EnvironmentVariableTarget.Process);
            }

            break;
        }
    }
}