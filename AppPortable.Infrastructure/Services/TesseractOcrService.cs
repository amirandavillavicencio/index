using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;

namespace AppPortable.Infrastructure.Services;

public sealed class TesseractOcrService : IOcrService
{
    public bool IsAvailable => ResolveTesseractExecutablePath() is not null;

    public Task<DocumentPage> ApplyOcrAsync(string sourcePdfPath, DocumentPage page, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!IsAvailable)
        {
            return Task.FromResult(page);
        }

        var warnings = page.Warnings.ToList();
        warnings.Add("OCR Tesseract no aplicado: renderizado por página no configurado en esta fase.");

        return Task.FromResult(new DocumentPage
        {
            PageNumber = page.PageNumber,
            ExtractionLayer = page.ExtractionLayer == ExtractionLayer.Failed ? ExtractionLayer.Failed : page.ExtractionLayer,
            OcrConfidence = page.OcrConfidence,
            Text = page.Text,
            TextLength = page.TextLength,
            Warnings = warnings
        });
    }

    public async Task<IReadOnlyList<DocumentPage>> ApplyOcrAsync(string sourcePdfPath, IReadOnlyList<DocumentPage> pages, CancellationToken cancellationToken = default)
    {
        var output = new List<DocumentPage>(pages.Count);
        foreach (var page in pages)
        {
            output.Add(await ApplyOcrAsync(sourcePdfPath, page, cancellationToken));
        }

        return output;
    }

    private static string? ResolveTesseractExecutablePath()
    {
        var command = OperatingSystem.IsWindows() ? "where" : "which";
        var arguments = "tesseract";

        try
        {
            var startInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = System.Diagnostics.Process.Start(startInfo);
            if (process is null)
            {
                return null;
            }

            process.WaitForExit(1500);
            if (process.ExitCode != 0)
            {
                return null;
            }

            var path = process.StandardOutput.ReadLine();
            return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        }
        catch
        {
            return null;
        }
    }
}
