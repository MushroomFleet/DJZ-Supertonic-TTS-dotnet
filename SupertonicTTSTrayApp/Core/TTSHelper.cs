using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ML.OnnxRuntime;

namespace SupertonicTTSTrayApp.Core
{
    public static class TTSHelper
    {
        public static TextToSpeech LoadTextToSpeech(string onnxDir)
        {
            var sessionOptions = new SessionOptions();

            var textEncoder = new InferenceSession(
                Path.Combine(onnxDir, "text_encoder.onnx"), sessionOptions);
            var durationPredictor = new InferenceSession(
                Path.Combine(onnxDir, "duration_predictor.onnx"), sessionOptions);
            var vectorEstimator = new InferenceSession(
                Path.Combine(onnxDir, "vector_estimator.onnx"), sessionOptions);
            var vocoder = new InferenceSession(
                Path.Combine(onnxDir, "vocoder.onnx"), sessionOptions);

            var unicodeIndexerPath = Path.Combine(onnxDir, "unicode_indexer.json");
            var unicodeProcessor = new UnicodeProcessor(unicodeIndexerPath);

            var configPath = Path.Combine(onnxDir, "tts.json");
            var config = LoadConfig(configPath);

            return new TextToSpeech(
                textEncoder,
                durationPredictor,
                vectorEstimator,
                vocoder,
                unicodeProcessor,
                config);
        }

        public static TTSConfig LoadConfig(string configPath)
        {
            var json = File.ReadAllText(configPath);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            return new TTSConfig
            {
                AE = new TTSConfig.AEConfig
                {
                    SampleRate = root.GetProperty("ae").GetProperty("sample_rate").GetInt32(),
                    BaseChunkSize = root.GetProperty("ae").GetProperty("base_chunk_size").GetInt32()
                },
                TTL = new TTSConfig.TTLConfig
                {
                    ChunkCompressFactor = root.GetProperty("ttl").GetProperty("chunk_compress_factor").GetInt32(),
                    LatentDim = root.GetProperty("ttl").GetProperty("latent_dim").GetInt32()
                }
            };
        }

        public static Style LoadVoiceStyle(List<string> stylePaths)
        {
            if (stylePaths.Count == 0)
                throw new ArgumentException("At least one style path is required");

            Console.WriteLine($"Loading voice style from: {stylePaths[0]}");
            var styleJson = File.ReadAllText(stylePaths[0]);
            var styleDict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(styleJson)
                ?? throw new Exception("Failed to load style");

            Console.WriteLine("Parsing style_ttl...");
            // Parse TTL (style_ttl)
            var ttlElement = styleDict["style_ttl"];
            var ttlData = FlattenJsonArray(ttlElement.GetProperty("data"));
            var ttlDims = ttlElement.GetProperty("dims").EnumerateArray()
                .Select(e => e.GetInt64()).ToArray();
            Console.WriteLine($"TTL data: {ttlData.Length} floats, dims: [{string.Join(", ", ttlDims)}]");

            Console.WriteLine("Parsing style_dp...");
            // Parse DP (style_dp)
            var dpElement = styleDict["style_dp"];
            var dpData = FlattenJsonArray(dpElement.GetProperty("data"));
            var dpDims = dpElement.GetProperty("dims").EnumerateArray()
                .Select(e => e.GetInt64()).ToArray();
            Console.WriteLine($"DP data: {dpData.Length} floats, dims: [{string.Join(", ", dpDims)}]");

            return new Style(ttlData, ttlDims, dpData, dpDims);
        }

        private static float[] FlattenJsonArray(JsonElement element)
        {
            var result = new List<float>();
            FlattenRecursive(element, result);
            return result.ToArray();
        }

        private static void FlattenRecursive(JsonElement element, List<float> result)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    FlattenRecursive(item, result);
                }
            }
            else if (element.ValueKind == JsonValueKind.Number)
            {
                result.Add(element.GetSingle());
            }
        }

        public static void WriteWavFile(string filename, float[] audioData, int sampleRate)
        {
            using var writer = new BinaryWriter(File.Create(filename));
            WriteWavToStream(writer, audioData, sampleRate);
        }

        public static void WriteWavToStream(BinaryWriter writer, float[] audioData, int sampleRate)
        {
            const int bitsPerSample = 16;
            const int channels = 1;
            int byteRate = sampleRate * channels * bitsPerSample / 8;
            int blockAlign = channels * bitsPerSample / 8;

            // Convert float to 16-bit PCM
            var pcmData = new short[audioData.Length];
            for (int i = 0; i < audioData.Length; i++)
            {
                float sample = Math.Max(-1.0f, Math.Min(1.0f, audioData[i]));
                pcmData[i] = (short)(sample * short.MaxValue);
            }

            int dataSize = pcmData.Length * sizeof(short);
            int fileSize = 36 + dataSize;

            // Write WAV header
            writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(fileSize);
            writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

            // Write fmt chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Chunk size
            writer.Write((short)1); // Audio format (1 = PCM)
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)blockAlign);
            writer.Write((short)bitsPerSample);

            // Write data chunk
            writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (var sample in pcmData)
            {
                writer.Write(sample);
            }
        }
    }
}
