#nullable enable
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShiritoriApp
{
    internal sealed class FakeGeminiService : IGeminiService
    {
        private readonly Queue<object> script = new Queue<object>();
        private readonly List<string> receivedPrompts = new List<string>();
        private readonly List<string> receivedReplies = new List<string>();

        public FakeGeminiService()
        {
        }

        public FakeGeminiService(IEnumerable<string> scriptedReplies)
        {
            if (scriptedReplies == null)
            {
                throw new ArgumentNullException(nameof(scriptedReplies));
            }

            foreach (string reply in scriptedReplies)
            {
                AddReply(reply);
            }
        }

        public List<string> ReceivedPrompts => new List<string>(receivedPrompts);
        public List<string> ReceivedReplies => new List<string>(receivedReplies);
        public int CallCount => receivedPrompts.Count;

        public void AddReply(string reply)
        {
            if (reply == null)
            {
                throw new ArgumentNullException(nameof(reply));
            }

            script.Enqueue(reply);
        }

        public void AddException(Exception exception)
        {
            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            script.Enqueue(exception);
        }

        public Task<string> AskAsync(string prompt)
        {
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("A prompt is required.", nameof(prompt));
            }

            receivedPrompts.Add(prompt);
            if (script.Count == 0)
            {
                var exception = new InvalidOperationException("The fake Gemini reply script is exhausted.");
                receivedReplies.Add("[Exception: " + exception.GetType().Name + ": " + exception.Message + "]");
                return Task.FromException<string>(exception);
            }

            object next = script.Dequeue();
            if (next is Exception scriptedException)
            {
                receivedReplies.Add("[Exception: " + scriptedException.GetType().Name + ": " + scriptedException.Message + "]");
                return Task.FromException<string>(scriptedException);
            }

            string reply = (string)next;
            receivedReplies.Add(reply);
            return Task.FromResult(reply);
        }
    }
}
