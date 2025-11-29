using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;

namespace SupertonicTTSTrayApp.Core
{
    public class TextToSpeech : IDisposable
    {
        private readonly InferenceSession _textEncoder;
        private readonly InferenceSession _durationPredictor;
        private readonly InferenceSession _vectorEstimator;
        private readonly InferenceSession _vocoder;
        private readonly UnicodeProcessor _unicodeProcessor;
        private readonly TTSConfig _config;

        public int SampleRate => _config.AE.SampleRate;
        private int BaseChunkSize => _config.AE.BaseChunkSize;
        private int ChunkCompressFactor => _config.TTL.ChunkCompressFactor;
        private int LatentDim => _config.TTL.LatentDim;

        public TextToSpeech(
            InferenceSession textEncoder,
            InferenceSession durationPredictor,
            InferenceSession vectorEstimator,
            InferenceSession vocoder,
            UnicodeProcessor unicodeProcessor,
            TTSConfig config)
        {
            _textEncoder = textEncoder;
            _durationPredictor = durationPredictor;
            _vectorEstimator = vectorEstimator;
            _vocoder = vocoder;
            _unicodeProcessor = unicodeProcessor;
            _config = config;
        }

        private (float[][][] noisyLatent, float[][][] latentMask) SampleNoisyLatent(float[] duration)
        {
            int bsz = duration.Length;
            Console.WriteLine($"SampleNoisyLatent: bsz={bsz}, duration={string.Join(", ", duration)}");
            Console.WriteLine($"SampleRate={SampleRate}, BaseChunkSize={BaseChunkSize}, ChunkCompressFactor={ChunkCompressFactor}, LatentDim={LatentDim}");

            if (bsz == 0 || duration.Length == 0)
            {
                throw new ArgumentException("Duration array is empty");
            }

            float wavLenMax = duration.Max() * SampleRate;
            var wavLengths = duration.Select(d => (long)(d * SampleRate)).ToArray();
            int chunkSize = BaseChunkSize * ChunkCompressFactor;
            Console.WriteLine($"chunkSize={chunkSize}, wavLenMax={wavLenMax}");

            int latentLen = (int)((wavLenMax + chunkSize - 1) / chunkSize);
            int latentDim = LatentDim * ChunkCompressFactor;
            Console.WriteLine($"latentLen={latentLen}, latentDim={latentDim}");

            // Generate random noise
            var random = new Random();
            var noisyLatent = new float[bsz][][];
            for (int b = 0; b < bsz; b++)
            {
                noisyLatent[b] = new float[latentDim][];
                for (int d = 0; d < latentDim; d++)
                {
                    noisyLatent[b][d] = new float[latentLen];
                    for (int t = 0; t < latentLen; t++)
                    {
                        // Box-Muller transform for normal distribution
                        double u1 = 1.0 - random.NextDouble();
                        double u2 = 1.0 - random.NextDouble();
                        noisyLatent[b][d][t] = (float)(Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Cos(2.0 * Math.PI * u2));
                    }
                }
            }

            var latentMask = GetLatentMask(wavLengths);

            // Apply mask
            for (int b = 0; b < bsz; b++)
            {
                for (int d = 0; d < latentDim; d++)
                {
                    for (int t = 0; t < latentLen; t++)
                    {
                        noisyLatent[b][d][t] *= latentMask[b][0][t];
                    }
                }
            }

            return (noisyLatent, latentMask);
        }

        private float[][][] GetLatentMask(long[] wavLengths)
        {
            int latentSize = BaseChunkSize * ChunkCompressFactor;
            var latentLengths = wavLengths.Select(len => (len + latentSize - 1) / latentSize).ToArray();
            return LengthToMask(latentLengths);
        }

        private float[][][] LengthToMask(long[] lengths, long maxLen = -1)
        {
            if (maxLen == -1)
            {
                maxLen = lengths.Max();
            }

            var mask = new float[lengths.Length][][];
            for (int i = 0; i < lengths.Length; i++)
            {
                mask[i] = new float[1][];
                mask[i][0] = new float[maxLen];
                for (int j = 0; j < maxLen; j++)
                {
                    mask[i][0][j] = j < lengths[i] ? 1.0f : 0.0f;
                }
            }
            return mask;
        }

        private DenseTensor<float> ArrayToTensor(float[][][] array, long[] dims)
        {
            var flat = new List<float>();
            foreach (var batch in array)
            {
                foreach (var row in batch)
                {
                    flat.AddRange(row);
                }
            }
            return new DenseTensor<float>(flat.ToArray(), dims.Select(x => (int)x).ToArray());
        }

        private DenseTensor<long> IntArrayToTensor(long[][] array, long[] dims)
        {
            var flat = new List<long>();
            foreach (var row in array)
            {
                flat.AddRange(row);
            }
            return new DenseTensor<long>(flat.ToArray(), dims.Select(x => (int)x).ToArray());
        }

        public (float[] wav, float[] duration) Call(
            string text,
            Style style,
            int totalSteps = 5,
            float speed = 1.05f,
            float silenceDuration = 0.3f)
        {
            var textList = new List<string> { text };
            int bsz = textList.Count;

            if (bsz != style.TtlShape[0])
            {
                throw new ArgumentException("Number of texts must match number of style vectors");
            }

            // Process text
            var (textIds, textMask) = _unicodeProcessor.Process(textList);
            var textIdsShape = new long[] { bsz, textIds[0].Length };
            var textMaskShape = new long[] { bsz, 1, textMask[0][0].Length };

            var textIdsTensor = IntArrayToTensor(textIds, textIdsShape);
            var textMaskTensor = ArrayToTensor(textMask, textMaskShape);

            var styleTtlTensor = new DenseTensor<float>(style.Ttl, style.TtlShape.Select(x => (int)x).ToArray());
            var styleDpTensor = new DenseTensor<float>(style.Dp, style.DpShape.Select(x => (int)x).ToArray());

            // Run duration predictor FIRST
            var dpInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("text_ids", textIdsTensor),
                NamedOnnxValue.CreateFromTensor("style_dp", styleDpTensor),
                NamedOnnxValue.CreateFromTensor("text_mask", textMaskTensor)
            };
            using var dpOutputs = _durationPredictor.Run(dpInputs);
            var durOnnx = dpOutputs.First(o => o.Name == "duration").AsTensor<float>().ToArray();

            Console.WriteLine($"Duration Predictor output: {durOnnx.Length} values");
            Console.WriteLine($"Duration values: {string.Join(", ", durOnnx.Take(5))}");
            Console.WriteLine($"Speed: {speed}");

            // Apply speed factor to duration
            for (int i = 0; i < durOnnx.Length; i++)
            {
                durOnnx[i] /= speed;
            }

            Console.WriteLine($"Duration after speed adjustment: {string.Join(", ", durOnnx.Take(5))}");

            // Run text encoder SECOND
            var textEncInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("text_ids", textIdsTensor),
                NamedOnnxValue.CreateFromTensor("style_ttl", styleTtlTensor),
                NamedOnnxValue.CreateFromTensor("text_mask", textMaskTensor)
            };
            using var textEncOutputs = _textEncoder.Run(textEncInputs);
            var textEmbTensor = textEncOutputs.First(o => o.Name == "text_emb").AsTensor<float>();

            // Sample noisy latent
            var (xt, latentMask) = SampleNoisyLatent(durOnnx);
            var latentShape = new long[] { bsz, xt[0].Length, xt[0][0].Length };
            var latentMaskShape = new long[] { bsz, 1, latentMask[0][0].Length };

            var totalStepArray = Enumerable.Repeat((float)totalSteps, bsz).ToArray();

            // Iterative denoising
            for (int step = 0; step < totalSteps; step++)
            {
                var currentStepArray = Enumerable.Repeat((float)step, bsz).ToArray();

                var vectorEstInputs = new List<NamedOnnxValue>
                {
                    NamedOnnxValue.CreateFromTensor("noisy_latent", ArrayToTensor(xt, latentShape)),
                    NamedOnnxValue.CreateFromTensor("text_emb", textEmbTensor),
                    NamedOnnxValue.CreateFromTensor("style_ttl", styleTtlTensor),
                    NamedOnnxValue.CreateFromTensor("text_mask", textMaskTensor),
                    NamedOnnxValue.CreateFromTensor("latent_mask", ArrayToTensor(latentMask, latentMaskShape)),
                    NamedOnnxValue.CreateFromTensor("total_step", new DenseTensor<float>(totalStepArray, new int[] { bsz })),
                    NamedOnnxValue.CreateFromTensor("current_step", new DenseTensor<float>(currentStepArray, new int[] { bsz }))
                };

                using var vectorEstOutputs = _vectorEstimator.Run(vectorEstInputs);
                var denoisedLatent = vectorEstOutputs.First(o => o.Name == "denoised_latent").AsTensor<float>();

                // Update xt
                int idx = 0;
                for (int b = 0; b < bsz; b++)
                {
                    for (int d = 0; d < xt[b].Length; d++)
                    {
                        for (int t = 0; t < xt[b][d].Length; t++)
                        {
                            xt[b][d][t] = denoisedLatent.GetValue(idx++);
                        }
                    }
                }
            }

            // Run vocoder
            var vocoderInputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("latent", ArrayToTensor(xt, latentShape))
            };
            using var vocoderOutputs = _vocoder.Run(vocoderInputs);
            var wavTensor = vocoderOutputs.First(o => o.Name == "wav_tts").AsTensor<float>();

            // Add silence
            var wav = wavTensor.ToArray();
            int silenceSamples = (int)(silenceDuration * SampleRate);
            var silence = new float[silenceSamples];
            var finalWav = wav.Concat(silence).ToArray();

            return (finalWav, durOnnx);
        }

        public void Dispose()
        {
            _textEncoder?.Dispose();
            _durationPredictor?.Dispose();
            _vectorEstimator?.Dispose();
            _vocoder?.Dispose();
        }
    }
}
