using Microsoft.Extensions.AI;
using Mscc.GenerativeAI.Microsoft;
using System;
using System.Threading.Tasks;

namespace ShiritoriApp
{
    internal sealed class GeminiService : IGeminiService
    {
        private readonly IChatClient chatClient;

        public GeminiService(string apiKey, string model)
        {
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                throw new ArgumentException("An API key is required.", nameof(apiKey));
            }

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("A model name is required.", nameof(model));
            }

            chatClient = new GeminiChatClient(apiKey: apiKey, model: model);
        }

        public async Task<string> AskAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("A prompt is required.", nameof(prompt));
            }

            var response = await chatClient.GetResponseAsync(prompt).ConfigureAwait(false);
            return response.Text;
        }
    }
}
