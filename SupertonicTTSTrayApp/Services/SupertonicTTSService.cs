using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using SupertonicTTSTrayApp.Core;

namespace SupertonicTTSTrayApp.Services
{
    public class SupertonicTTSService : IDisposable
    {
        private readonly string _modelsDir;
        private readonly ModelDownloader _downloader;
        private TextToSpeech? _ttsEngine;
        private Style? _currentStyle;
        private bool _isInitialized;

        public SupertonicTTSService(string? modelsDir = null)
        {
            _modelsDir = modelsDir ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SupertonicTTS", "Models");

            _downloader = new ModelDownloader(_modelsDir);
            _downloader.ProgressChanged += (s, e) =>
            {
                DownloadProgress?.Invoke(this, e);
            };
        }

        public event EventHandler<DownloadProgressEventArgs>? DownloadProgress;

        public bool IsInitialized => _isInitialized;
        public int SampleRate => _ttsEngine?.SampleRate ?? 44100;

        /// <summary>
        /// Initialize the TTS engine (downloads models if needed)
        /// </summary>
        public async Task<bool> InitializeAsync(
            CancellationToken cancellationToken = default)
        {
            if (_isInitialized)
                return true;

            try
            {
                Console.WriteLine("Starting TTS initialization...");

                // Download models if needed
                Console.WriteLine("Checking/downloading models...");
                if (!await _downloader.EnsureModelsDownloadedAsync(cancellationToken))
                {
                    Console.WriteLine("Model download failed");
                    throw new Exception("Failed to download models. Check internet connection and firewall settings.");
                }

                Console.WriteLine("Models ready. Loading TTS engine...");

                // Load TTS engine
                var onnxDir = Path.Combine(_modelsDir, "onnx");
                Console.WriteLine($"ONNX directory: {onnxDir}");

                _ttsEngine = TTSHelper.LoadTextToSpeech(onnxDir);
                Console.WriteLine("TTS engine loaded successfully");

                // Load default voice style
                Console.WriteLine("Loading default voice style (Male1)...");
                await LoadVoiceStyleAsync(VoiceStyle.Male1, cancellationToken);
                Console.WriteLine("Voice style loaded successfully");

                _isInitialized = true;
                Console.WriteLine("TTS initialization complete!");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Initialization failed: {ex.GetType().Name}: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw; // Re-throw to let the UI handle it
            }
        }

        /// <summary>
        /// Load a specific voice style
        /// </summary>
        public async Task LoadVoiceStyleAsync(
            VoiceStyle voice,
            CancellationToken cancellationToken = default)
        {
            var voiceFile = voice switch
            {
                VoiceStyle.Male1 => "M1.json",
                VoiceStyle.Male2 => "M2.json",
                VoiceStyle.Female1 => "F1.json",
                VoiceStyle.Female2 => "F2.json",
                _ => "M1.json"
            };

            var stylePath = Path.Combine(_modelsDir, "voice_styles", voiceFile);
            _currentStyle = await Task.Run(() =>
                TTSHelper.LoadVoiceStyle(new List<string> { stylePath }),
                cancellationToken);
        }

        /// <summary>
        /// Synthesize speech from text
        /// </summary>
        public async Task<TTSSynthesisResult> SynthesizeAsync(
            string text,
            TTSSynthesisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || _ttsEngine == null || _currentStyle == null)
            {
                throw new InvalidOperationException(
                    "TTS not initialized. Call InitializeAsync() first.");
            }

            options ??= new TTSSynthesisOptions();

            return await Task.Run(() =>
            {
                var (wav, duration) = _ttsEngine.Call(
                    text,
                    _currentStyle,
                    options.TotalSteps,
                    options.Speed,
                    options.SilenceDuration);

                return new TTSSynthesisResult
                {
                    AudioData = wav,
                    Duration = duration[0],
                    SampleRate = _ttsEngine.SampleRate,
                    Text = text
                };
            }, cancellationToken);
        }

        /// <summary>
        /// Synthesize and save to WAV file
        /// </summary>
        public async Task<string> SynthesizeToFileAsync(
            string text,
            string outputPath,
            TTSSynthesisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var result = await SynthesizeAsync(text, options, cancellationToken);

            await Task.Run(() =>
            {
                TTSHelper.WriteWavFile(outputPath, result.AudioData, result.SampleRate);
            }, cancellationToken);

            return outputPath;
        }

        /// <summary>
        /// Get audio data as byte array (for streaming)
        /// </summary>
        public async Task<byte[]> SynthesizeToWavBytesAsync(
            string text,
            TTSSynthesisOptions? options = null,
            CancellationToken cancellationToken = default)
        {
            var result = await SynthesizeAsync(text, options, cancellationToken);

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // Write WAV header and data
            TTSHelper.WriteWavToStream(writer, result.AudioData, result.SampleRate);

            return ms.ToArray();
        }

        public void Dispose()
        {
            // Cleanup ONNX sessions if needed
            _ttsEngine?.Dispose();
            _currentStyle = null;
            _isInitialized = false;
        }
    }

    // Supporting types
    public enum VoiceStyle
    {
        Male1,
        Male2,
        Female1,
        Female2
    }

    public class TTSSynthesisOptions
    {
        public int TotalSteps { get; set; } = 5;
        public float Speed { get; set; } = 1.05f;
        public float SilenceDuration { get; set; } = 0.3f;
    }

    public class TTSSynthesisResult
    {
        public float[] AudioData { get; set; } = Array.Empty<float>();
        public float Duration { get; set; }
        public int SampleRate { get; set; }
        public string Text { get; set; } = "";
    }
}
