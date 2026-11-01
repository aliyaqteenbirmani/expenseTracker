using Microsoft.Extensions.Configuration;
using SpendwiseSystem.Application.Services.ReceiptOcrService;
using System.Diagnostics;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.Pix;

namespace SpendwiseSystem.Infrastructure.Services.ReceiptOcrService
{
    public class TesseractReceiptOcrService : IReceiptOcrService
    {
        private const int MaxPdfPages = 3;
        private readonly string _tessdataPath;
        private readonly string _pdfToPpmPath;

        public TesseractReceiptOcrService(IConfiguration configuration)
        {
            var configuredTessdataPath = configuration["ReceiptOcr:TessdataPath"];
            _tessdataPath = string.IsNullOrWhiteSpace(configuredTessdataPath)
                ? Path.Combine(AppContext.BaseDirectory, "tessdata")
                : Path.GetFullPath(configuredTessdataPath);
            _pdfToPpmPath = configuration["ReceiptOcr:PdfToPpmPath"];
        }

        public async Task<ReceiptOcrResult> ExtractTextAsync(string filePath, string contentType)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Receipt file was not found.");

            if (!Directory.Exists(_tessdataPath) || !File.Exists(Path.Combine(_tessdataPath, "eng.traineddata")))
                throw new InvalidOperationException("Receipt OCR is not configured: English Tesseract data is missing.");

            string temporaryDirectory = null;
            try
            {
                IReadOnlyList<string> imagePaths;
                if (contentType == "application/pdf")
                {
                    temporaryDirectory = Path.Combine(Path.GetTempPath(), $"SpendwiseReceiptOcr-{Guid.NewGuid():N}");
                    Directory.CreateDirectory(temporaryDirectory);
                    imagePaths = await RenderPdfPagesAsync(filePath, temporaryDirectory);
                }
                else
                {
                    imagePaths = new[] { filePath };
                }

                using var engine = new Engine(_tessdataPath, Language.English, EngineMode.Default);
                var pageTexts = new List<string>();
                var confidenceTotal = 0d;
                var pageCountWithText = 0;

                foreach (var imagePath in imagePaths)
                {
                    using var image = Image.LoadFromFile(imagePath);
                    using var page = engine.Process(image);
                    var text = page.Text?.Trim();
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        pageTexts.Add(text);
                        confidenceTotal += page.MeanConfidence;
                        pageCountWithText++;
                    }
                }

                return new ReceiptOcrResult
                {
                    Text = string.Join(Environment.NewLine + Environment.NewLine, pageTexts),
                    Confidence = pageCountWithText == 0 ? 0 : Math.Clamp(confidenceTotal / pageCountWithText, 0, 1)
                };
            }
            finally
            {
                if (temporaryDirectory != null && Directory.Exists(temporaryDirectory))
                    Directory.Delete(temporaryDirectory, recursive: true);
            }
        }

        private async Task<IReadOnlyList<string>> RenderPdfPagesAsync(string pdfPath, string temporaryDirectory)
        {
            var outputPrefix = Path.Combine(temporaryDirectory, "page");
            var executable = ResolvePdfToPpmExecutable();
            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };
            startInfo.ArgumentList.Add("-f");
            startInfo.ArgumentList.Add("1");
            startInfo.ArgumentList.Add("-l");
            startInfo.ArgumentList.Add(MaxPdfPages.ToString());
            startInfo.ArgumentList.Add("-png");
            startInfo.ArgumentList.Add("-r");
            startInfo.ArgumentList.Add("200");
            startInfo.ArgumentList.Add(pdfPath);
            startInfo.ArgumentList.Add(outputPrefix);

            using var process = new Process { StartInfo = startInfo };
            try
            {
                if (!process.Start())
                    throw new InvalidOperationException("Could not start the PDF renderer required for receipt OCR.");
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                throw new InvalidOperationException("PDF receipt OCR requires Poppler's pdftoppm executable. Configure ReceiptOcr:PdfToPpmPath or add pdftoppm to PATH.", ex);
            }

            var standardErrorTask = process.StandardError.ReadToEndAsync();
            var standardOutputTask = process.StandardOutput.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                throw new InvalidOperationException("PDF receipt rendering timed out.");
            }

            var error = await standardErrorTask;
            await standardOutputTask;
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"PDF receipt rendering failed: {error.Trim()}");

            var renderedPages = Directory.GetFiles(temporaryDirectory, "page-*.png")
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
            if (renderedPages.Length == 0)
                throw new InvalidOperationException("The PDF did not contain any renderable pages.");

            return renderedPages;
        }

        private string ResolvePdfToPpmExecutable()
        {
            if (string.IsNullOrWhiteSpace(_pdfToPpmPath))
                return OperatingSystem.IsWindows() ? "pdftoppm.exe" : "pdftoppm";

            var configuredPath = Path.GetFullPath(_pdfToPpmPath);
            if (Directory.Exists(configuredPath))
                return Path.Combine(configuredPath, OperatingSystem.IsWindows() ? "pdftoppm.exe" : "pdftoppm");

            return configuredPath;
        }
    }
}
