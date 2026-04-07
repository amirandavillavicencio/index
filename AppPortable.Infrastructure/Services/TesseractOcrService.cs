using System.Diagnostics;
using System.Runtime.Versioning;
using System.Text;
using AppPortable.Core.Enums;
using AppPortable.Core.Interfaces;
using AppPortable.Core.Models;
using PDFtoImage;
using SkiaSharp;

namespace AppPortable.Infrastructure.Services;

public sealed class TesseractOcrService : IOcrService
{
    public bool IsAvailable => PortablePathResolver.ResolveTesseractExecutablePathPortableFirst() is not null;

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    public async Task<DocumentPage> ApplyOcrAsync(
        string sourcePdfPath,
        DocumentPage page,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var warnings = page.Warnings.ToList();
        var tesseractPath = PortablePathResolver.ResolveTesseractExecutablePathPortableFirst();
        if (tesseractPath is null)
        {
            warnings.Add($"OCR deshabilitado: falta tesseract/tesseract.exe en '{PortablePathResolver.GetPortableTesseractExecutablePath()}' y no se encontró fallback en PATH.");
            return ClonePage(page, warnings);
        }

        var hasPortableSpanishModel = File.Exists(PortablePathResolver.GetPortableSpanishLanguageModelPath());
        if (!hasPortableSpanishModel)
        {
            warnings.Add($"OCR advertencia: falta tesseract/tessdata/spa.traineddata en '{PortablePathResolver.GetPortableSpanishLanguageModelPath()}'. Se usará fallback de idioma.");
        }

        var tempDirectory = Path.GetTempPath();
        var tempPng = Path.Combine(tempDirectory, $"ocr_{page.PageNumber}_{Guid.NewGuid():N}.png");
        var tempBase = Path.Combine(tempDirectory, $"ocr_{page.PageNumber}_{Guid.NewGuid():N}");
        var tempTxt = $"{tempBase}.txt";

        try
        {
            await RenderPageToPngAsync(sourcePdfPath, page.PageNumber - 1, tempPng, cancellationToken);

            var ocrText = (await RunTesseractToFileWithFallbackAsync(
                tesseractPath,
                tempPng,
                tempBase,
                hasPortableSpanishModel,
                cancellationToken)).Trim();

            var finalText = string.IsNullOrWhiteSpace(ocrText)
                ? page.Text
                : ocrText;

            var finalLayer = string.IsNullOrWhiteSpace(ocrText)
                ? page.ExtractionLayer
                : ExtractionLayer.Ocr;

            return new DocumentPage
            {
                PageNumber = page.PageNumber,
                ExtractionLayer = finalLayer,
                OcrConfidence = null,
                Text = finalText,
                TextLength = finalText.Length,
                Warnings = warnings
            };
        }
        catch (Exception ex)
        {
            warnings.Add($"OCR error pag. {page.PageNumber}: {ex.Message}");
            return ClonePage(page, warnings);
        }
        finally
        {
            TryDelete(tempPng);
            TryDelete(tempTxt);
        }
    }

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
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

    [SupportedOSPlatform("windows")]
    [SupportedOSPlatform("linux")]
    [SupportedOSPlatform("osx")]
    private static Task RenderPageToPngAsync(
        string pdfPath,
        int pageIndex,
        string outputPng,
        CancellationToken ct)
    {
        return Task.Run(() =>
        {
            var pdfBase64 = Convert.ToBase64String(File.ReadAllBytes(pdfPath));
            var options = new RenderOptions
            {
                Dpi = 300,
                WithAspectRatio = true
            };

            using var bitmap = Conversion.ToImage(pdfBase64, null, pageIndex, options);
            using var fs = File.Open(outputPng, FileMode.Create, FileAccess.Write, FileShare.None);
            bitmap.Encode(fs, SKEncodedImageFormat.Png, 100);
        }, ct);
    }

    private static async Task<string> RunTesseractToFileWithFallbackAsync(
        string tesseractPath,
        string imagePath,
        string outputBasePath,
        bool hasPortableSpanishModel,
        CancellationToken ct)
    {
        if (!hasPortableSpanishModel)
        {
            return await RunTesseractToFileAsync(tesseractPath, imagePath, outputBasePath, "eng", ct);
        }

        try
        {
            return await RunTesseractToFileAsync(tesseractPath, imagePath, outputBasePath, "spa", ct);
        }
        catch (InvalidOperationException)
        {
            return await RunTesseractToFileAsync(tesseractPath, imagePath, outputBasePath, "eng", ct);
        }
    }

    private static async Task<string> RunTesseractToFileAsync(
        string tesseractPath,
        string imagePath,
        string outputBasePath,
        string language,
        CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = tesseractPath,
            Arguments = $"\"{imagePath}\" \"{outputBasePath}\" -l {language}",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var process = new Process { StartInfo = psi };
        process.Start();

        var stdoutTask = process.StandardOutput.ReadToEndAsync(ct);
        var stderrTask = process.StandardError.ReadToEndAsync(ct);

        await process.WaitForExitAsync(ct);

        var stdout = await stdoutTask;
        var stderr = await stderrTask;

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(stderr)
                    ? $"Tesseract terminó con código {process.ExitCode}."
                    : stderr.Trim());
        }

        var txtPath = $"{outputBasePath}.txt";
        if (!File.Exists(txtPath))
        {
            return string.Empty;
        }

        var bytes = await File.ReadAllBytesAsync(txtPath, ct);

        string text;
        if (HasUtf8Bom(bytes))
        {
            text = Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        else
        {
            text = Encoding.UTF8.GetString(bytes);
        }

        return text.Replace("\r\n", "\n").Replace("\r", "\n").Normalize(NormalizationForm.FormC);
    }

    private static bool HasUtf8Bom(byte[] bytes)
    {
        return bytes.Length >= 3
               && bytes[0] == 0xEF
               && bytes[1] == 0xBB
               && bytes[2] == 0xBF;
    }

    private static DocumentPage ClonePage(DocumentPage page, IReadOnlyList<string> warnings)
    {
        return new DocumentPage
        {
            PageNumber = page.PageNumber,
            ExtractionLayer = page.ExtractionLayer,
            OcrConfidence = page.OcrConfidence,
            Text = page.Text,
            TextLength = page.TextLength,
            Warnings = warnings
        };
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
        }
    }
}
