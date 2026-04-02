using System.Diagnostics;
using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using PDFtoImage;
using SkiaSharp;

namespace AppPortable.Infrastructure.Services;

public sealed class TesseractOcrService : IOcrService
{
    public bool IsAvailable => ResolveTesseractExecutablePath() is not null;

    public async Task<DocumentPage> ApplyOcrAsync(
        string sourcePdfPath,
        DocumentPage page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var tesseractPath = ResolveTesseractExecutablePath();
        if (tesseractPath is null)
        {
            var w = page.Warnings.ToList();
            w.Add("Tesseract no encontrado en PATH. Instala Tesseract y agrégalo al PATH.");
            return new DocumentPage
            {
                PageNumber      = page.PageNumber,
                ExtractionLayer = page.ExtractionLayer,
                OcrConfidence   = page.OcrConfidence,
                Text            = page.Text,
                TextLength      = page.TextLength,
                Warnings        = w
            };
        }

        var tempPng = Path.Combine(
            Path.GetTempPath(),
            $"ocr_{page.PageNumber}_{Guid.NewGuid():N}.png");

        try
        {
            await RenderPageToPngAsync(sourcePdfPath, page.PageNumber - 1, tempPng, cancellationToken);

            var ocrText = await RunTesseractAsync(tesseractPath, tempPng, cancellationToken);
            var finalText = string.IsNullOrWhiteSpace(ocrText) ? page.Text : ocrText.Trim();
            var layer     = string.IsNullOrWhiteSpace(ocrText) ? ExtractionLayer.Failed : ExtractionLayer.Ocr;

            return new DocumentPage
            {
                PageNumber      = page.PageNumber,
                ExtractionLayer = layer,
                OcrConfidence   = null,
                Text            = finalText,
                TextLength      = finalText.Length,
                Warnings        = page.Warnings
            };
        }
        catch (Exception ex)
        {
            var w = page.Warnings.ToList();
            w.Add($"OCR error pág. {page.PageNumber}: {ex.Message}");
            return new DocumentPage
            {
                PageNumber      = page.PageNumber,
                ExtractionLayer = ExtractionLayer.Failed,
                OcrConfidence   = null,
                Text            = page.Text,
                TextLength      = page.TextLength,
                Warnings        = w
            };
        }
        finally
        {
            if (File.Exists(tempPng)) File.Delete(tempPng);
        }
    }

    public async Task<IReadOnlyList<DocumentPage>> ApplyOcrAsync(
        string sourcePdfPath,
        IReadOnlyList<DocumentPage> pages,
        CancellationToken cancellationToken = default)
    {
        var output = new List<DocumentPage>(pages.Count);
        foreach (var page in pages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            output.Add(await ApplyOcrAsync(sourcePdfPath, page, cancellationToken));
        }
        return output;
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static Task RenderPageToPngAsync(
        string pdfPath, int pageIndex, string outputPng, CancellationToken ct)
    {
        return Task.Run(() =>
        {
            using var stream = File.OpenRead(pdfPath);
            using var bitmap = Conversion.ToImage(stream, pageIndex, dpi: 300);
            using var fs     = File.OpenWrite(outputPng);
            bitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
        }, ct);
    }

    private static async Task<string> RunTesseractAsync(
        string tesseractPath, string imagePath, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName               = tesseractPath,
            Arguments              = $"\"{imagePath}\" stdout -l spa+eng txt",
            RedirectStandardOutput = true,
            RedirectStandardError  = true,
            UseShellExecute        = false,
            CreateNoWindow         = true
        };

        using var proc = new Process { StartInfo = psi };
        proc.Start();
        var output = await proc.StandardOutput.ReadToEndAsync(ct);
        await proc.WaitForExitAsync(ct);
        return output;
    }

    private static string? ResolveTesseractExecutablePath()
    {
        var command = OperatingSystem.IsWindows() ? "where" : "which";
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName               = command,
                Arguments              = "tesseract",
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                UseShellExecute        = false,
                CreateNoWindow         = true
            };
            using var proc = Process.Start(psi);
            if (proc is null) return null;
            proc.WaitForExit(1500);
            if (proc.ExitCode != 0) return null;
            var path = proc.StandardOutput.ReadLine();
            return string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        }
        catch { return null; }
    }
}
