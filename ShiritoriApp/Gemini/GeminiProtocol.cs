#nullable enable
using System;
using System.Collections.Generic;
using System.Text.Json;

namespace ShiritoriApp
{
    internal sealed class GeminiProtocol
    {
        private const string Instructions = "We are playing English Shiritori. Each word must start with the last letter of the previous word. No word may be used twice.\n\n" +
            "I will send a JSON object with these keys:\n" +
            "\"playerWord\": the player's word, possibly misspelled\n" +
            "\"usedWords\": all earlier words in the game\n\n" +
            "Fix the spelling of playerWord, but do not replace it with a different word. Then reply with a word that starts with the last letter of the corrected word, is not in usedWords, and is not the corrected word. Use only common dictionary words, in lowercase letters, one word.\n\n" +
            "Reply with only a JSON object, with no extra text, using these keys: \"status\" (\"ok\", \"notAWord\" or \"noWordLeft\"), \"correctedWord\", \"replyWord\", and \"explanation\". Use null for every key that does not apply. Use \"notAWord\" if playerWord is not an English word you can recognize even after fixing the spelling. Use \"noWordLeft\" if you cannot find a valid reply.";

        public string BuildPrompt(List<string> usedWords, string playerWord)
        {
            if (usedWords == null)
            {
                throw new ArgumentNullException(nameof(usedWords));
            }

            if (playerWord == null)
            {
                throw new ArgumentNullException(nameof(playerWord));
            }

            var request = new GeminiRequest
            {
                PlayerWord = playerWord,
                UsedWords = usedWords
            };
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };

            return Instructions + "\n\n" + JsonSerializer.Serialize(request, options);
        }

        public GeminiReply ParseReply(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new FormatException("Gemini returned an empty reply.");
            }

            string json = ExtractJson(text);
            try
            {
                using (JsonDocument document = JsonDocument.Parse(json))
                {
                    JsonElement root = document.RootElement;
                    if (root.ValueKind != JsonValueKind.Object)
                    {
                        throw new FormatException("Gemini reply must be a JSON object.");
                    }

                    if (!root.TryGetProperty("status", out JsonElement statusElement) || statusElement.ValueKind != JsonValueKind.String)
                    {
                        throw new FormatException("Gemini reply is missing a text status.");
                    }

                    ReplyStatus status = ParseStatus(statusElement.GetString());
                    return new GeminiReply(
                        status,
                        ReadOptionalString(root, "correctedWord"),
                        ReadOptionalString(root, "replyWord"),
                        ReadOptionalString(root, "explanation"));
                }
            }
            catch (JsonException exception)
            {
                throw new FormatException("Gemini returned invalid JSON.", exception);
            }
        }

        private static string ExtractJson(string text)
        {
            string trimmed = text.Trim();
            if (!trimmed.StartsWith("```", StringComparison.Ordinal))
            {
                return trimmed;
            }

            int firstBrace = trimmed.IndexOf('{');
            int lastBrace = trimmed.LastIndexOf('}');
            if (firstBrace < 0 || lastBrace < firstBrace)
            {
                throw new FormatException("Gemini code fence does not contain a JSON object.");
            }

            return trimmed.Substring(firstBrace, lastBrace - firstBrace + 1);
        }

        private static ReplyStatus ParseStatus(string? status)
        {
            if (string.Equals(status, "ok", StringComparison.OrdinalIgnoreCase))
            {
                return ReplyStatus.Ok;
            }

            if (string.Equals(status, "notAWord", StringComparison.OrdinalIgnoreCase))
            {
                return ReplyStatus.NotAWord;
            }

            if (string.Equals(status, "noWordLeft", StringComparison.OrdinalIgnoreCase))
            {
                return ReplyStatus.NoWordLeft;
            }

            throw new FormatException("Gemini returned an unknown status.");
        }

        private static string? ReadOptionalString(JsonElement root, string propertyName)
        {
            if (!root.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind == JsonValueKind.Null)
            {
                return null;
            }

            if (value.ValueKind != JsonValueKind.String)
            {
                throw new FormatException("Gemini reply property '" + propertyName + "' must be a string or null.");
            }

            return value.GetString();
        }

        private sealed class GeminiRequest
        {
            public string PlayerWord { get; set; } = string.Empty;
            public List<string> UsedWords { get; set; } = new List<string>();
        }
    }
}
