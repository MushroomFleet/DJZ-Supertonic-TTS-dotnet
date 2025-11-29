using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace SupertonicTTSTrayApp.Services
{
    public class ModelDownloader
    {
        private const string HF_BASE_URL = "https://huggingface.co/Supertone/supertonic/resolve/main";
        private readonly HttpClient _httpClient;
        private readonly string _modelsDir;

        public ModelDownloader(string modelsDir)
        {
            _modelsDir = modelsDir;
            _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        }

        public event EventHandler<DownloadProgressEventArgs>? ProgressChanged;

        public async Task<bool> EnsureModelsDownloadedAsync(
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Check if models already exist
                if (AreModelsPresent())
                {
                    Console.WriteLine("Models already downloaded.");
                    return true;
                }

                Console.WriteLine("Downloading Supertonic TTS models...");
                await DownloadAllModelsAsync(cancellationToken);

                return AreModelsPresent();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error downloading models: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        private bool AreModelsPresent()
        {
            var requiredFiles = new[]
            {
                "onnx/duration_predictor.onnx",
                "onnx/text_encoder.onnx",
                "onnx/vector_estimator.onnx",
                "onnx/vocoder.onnx",
                "onnx/tts.json",
                "onnx/unicode_indexer.json",
                "voice_styles/M1.json",
                "voice_styles/M2.json",
                "voice_styles/F1.json",
                "voice_styles/F2.json"
            };

            foreach (var file in requiredFiles)
            {
                var fullPath = Path.Combine(_modelsDir, file);
                if (!File.Exists(fullPath))
                    return false;
            }

            return true;
        }

        private async Task DownloadAllModelsAsync(CancellationToken cancellationToken)
        {
            var files = new[]
            {
                ("onnx/duration_predictor.onnx", 1.6),
                ("onnx/text_encoder.onnx", 28.0),
                ("onnx/vector_estimator.onnx", 132.5),
                ("onnx/vocoder.onnx", 101.4),
                ("onnx/tts.json", 0.01),
                ("onnx/unicode_indexer.json", 0.3),
                ("voice_styles/M1.json", 0.4),
                ("voice_styles/M2.json", 0.4),
                ("voice_styles/F1.json", 0.4),
                ("voice_styles/F2.json", 0.4)
            };

            int currentFile = 0;
            double totalSize = 0;
            foreach (var (_, size) in files)
                totalSize += size;

            double downloadedSize = 0;

            foreach (var (relativePath, fileSize) in files)
            {
                currentFile++;
                var url = $"{HF_BASE_URL}/{relativePath}";
                var localPath = Path.Combine(_modelsDir, relativePath);

                // Create directory if needed
                var directory = Path.GetDirectoryName(localPath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                Console.WriteLine($"[{currentFile}/{files.Length}] Downloading {Path.GetFileName(relativePath)}...");

                await DownloadFileAsync(url, localPath, cancellationToken);

                downloadedSize += fileSize;
                var progress = (int)((downloadedSize / totalSize) * 100);
                ProgressChanged?.Invoke(this, new DownloadProgressEventArgs
                {
                    CurrentFile = currentFile,
                    TotalFiles = files.Length,
                    FileName = Path.GetFileName(relativePath),
                    PercentComplete = progress
                });
            }

            Console.WriteLine("All models downloaded successfully!");
        }

        private async Task DownloadFileAsync(
            string url,
            string destinationPath,
            CancellationToken cancellationToken)
        {
            using var response = await _httpClient.GetAsync(url,
                HttpCompletionOption.ResponseHeadersRead, cancellationToken);

            response.EnsureSuccessStatusCode();

            using var streamToReadFrom = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var streamToWriteTo = File.Open(destinationPath, FileMode.Create);

            await streamToReadFrom.CopyToAsync(streamToWriteTo, cancellationToken);
        }
    }

    public class DownloadProgressEventArgs : EventArgs
    {
        public int CurrentFile { get; set; }
        public int TotalFiles { get; set; }
        public string FileName { get; set; } = "";
        public int PercentComplete { get; set; }
    }
}
