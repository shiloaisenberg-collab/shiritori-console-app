#nullable enable

namespace ShiritoriApp
{
    public enum ReplyStatus
    {
        Ok = 1,
        NotAWord = 2,
        NoWordLeft = 3,
    }

    internal sealed class GeminiReply
    {
        public GeminiReply(ReplyStatus status, string? correctedWord, string? replyWord, string? explanation)
        {
            Status = status;
            CorrectedWord = correctedWord;
            ReplyWord = replyWord;
            Explanation = explanation;
        }

        public ReplyStatus Status { get; }
        public string? CorrectedWord { get; }
        public string? ReplyWord { get; }
        public string? Explanation { get; }
    }
}
