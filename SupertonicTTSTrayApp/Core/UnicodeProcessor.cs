using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SupertonicTTSTrayApp.Core
{
    public class UnicodeProcessor
    {
        private readonly Dictionary<int, long> _indexer;

        public UnicodeProcessor(string unicodeIndexerPath)
        {
            var json = File.ReadAllText(unicodeIndexerPath);
            var indexerArray = JsonSerializer.Deserialize<long[]>(json)
                ?? throw new Exception("Failed to load indexer");

            _indexer = new Dictionary<int, long>();
            for (int i = 0; i < indexerArray.Length; i++)
            {
                _indexer[i] = indexerArray[i];
            }
        }

        public (long[][] textIds, float[][][] textMask) Process(List<string> textList)
        {
            var processedTexts = textList.Select(PreprocessText).ToList();
            var textIdsLengths = processedTexts.Select(t => (long)t.Length).ToArray();
            long maxLen = textIdsLengths.Max();

            var textIds = new long[textList.Count][];
            for (int i = 0; i < processedTexts.Count; i++)
            {
                textIds[i] = new long[maxLen];
                var unicodeVals = processedTexts[i].Select(c => (int)c).ToArray();
                for (int j = 0; j < unicodeVals.Length; j++)
                {
                    if (_indexer.TryGetValue(unicodeVals[j], out long val))
                    {
                        textIds[i][j] = val;
                    }
                }
            }

            var textMask = GetTextMask(textIdsLengths);
            return (textIds, textMask);
        }

        private string PreprocessText(string text)
        {
            // Normalize
            text = text.Normalize(NormalizationForm.FormKD);

            // Remove emojis
            text = RemoveEmojis(text);

            // Replace symbols
            var replacements = new Dictionary<string, string>
            {
                {"–", "-"}, {"‑", "-"}, {"—", "-"},
                {"\u201C", "\""}, {"\u201D", "\""},
                {"\u2018", "'"}, {"\u2019", "'"},
                {"_", " "}, {"|", " "}, {"/", " "},
                {"#", " "}, {"→", " "}, {"←", " "}
            };

            foreach (var (k, v) in replacements)
                text = text.Replace(k, v);

            // Fix punctuation spacing
            text = Regex.Replace(text, @" ([,\.!?;:])", "$1");

            // Remove extra spaces
            text = Regex.Replace(text, @"\s+", " ").Trim();

            // Add period if needed
            if (!Regex.IsMatch(text, @"[.!?;:,'""\)\]}…。」』】〉》›»]$"))
                text += ".";

            return text;
        }

        private static string RemoveEmojis(string text)
        {
            var result = new StringBuilder();
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint;
                if (char.IsHighSurrogate(text[i]) && i + 1 < text.Length)
                {
                    codePoint = char.ConvertToUtf32(text[i], text[i + 1]);
                    i++;
                }
                else
                {
                    codePoint = text[i];
                }

                bool isEmoji = (codePoint >= 0x1F600 && codePoint <= 0x1F64F) ||
                               (codePoint >= 0x1F300 && codePoint <= 0x1F5FF) ||
                               (codePoint >= 0x1F680 && codePoint <= 0x1F6FF);

                if (!isEmoji)
                {
                    result.Append(codePoint > 0xFFFF
                        ? char.ConvertFromUtf32(codePoint)
                        : ((char)codePoint).ToString());
                }
            }
            return result.ToString();
        }

        private float[][][] GetTextMask(long[] textIdsLengths)
        {
            long maxLen = textIdsLengths.Max();
            var mask = new float[textIdsLengths.Length][][];

            for (int i = 0; i < textIdsLengths.Length; i++)
            {
                mask[i] = new float[1][];
                mask[i][0] = new float[maxLen];
                for (int j = 0; j < maxLen; j++)
                {
                    mask[i][0][j] = j < textIdsLengths[i] ? 1.0f : 0.0f;
                }
            }
            return mask;
        }
    }
}
