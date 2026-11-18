#nullable enable
using System;
using System.Threading.Tasks;

namespace ShiritoriApp
{
    public enum GameState
    {
        InProgress,
        PlayerWon,
        GeminiWon,
    }

    internal sealed class Shiritori
    {
        public const int MaxRetries = 2;

        private readonly IGeminiService service;
        private readonly GeminiProtocol protocol = new GeminiProtocol();

        public Shiritori(IGeminiService service)
        {
            this.service = service ?? throw new ArgumentNullException(nameof(service));
            History = new TurnHistory();
            State = GameState.InProgress;
        }

        public TurnHistory History { get; }
        public GameState State { get; private set; }
        public char? RequiredLetter => History.LastTurn?.LastLetter;

        internal async Task<MoveResult> PlayAsync(string? word)
        {
            if (State != GameState.InProgress)
            {
                throw new InvalidOperationException("The game is over. Start a new game before playing again.");
            }

            string originalWord = word ?? string.Empty;
            if (!IsWellFormed(originalWord))
            {
                return new MoveResult(MoveOutcome.NotWellFormed);
            }

            for (int attempt = 0; attempt <= MaxRetries; attempt++)
            {
                string prompt = protocol.BuildPrompt(History.UsedWords(), originalWord);
                string response = await service.AskAsync(prompt).ConfigureAwait(false);

                GeminiReply reply;
                try
                {
                    reply = protocol.ParseReply(response);
                }
                catch (FormatException)
                {
                    if (attempt == MaxRetries)
                    {
                        throw;
                    }

                    continue;
                }

                if (reply.Status == ReplyStatus.NotAWord)
                {
                    State = GameState.GeminiWon;
                    return new MoveResult(MoveOutcome.NotAWord);
                }

                string correctedWord;
                try
                {
                    correctedWord = NormalizeRequiredWord(reply.CorrectedWord, "correctedWord");
                }
                catch (FormatException)
                {
                    if (attempt == MaxRetries)
                    {
                        throw;
                    }

                    continue;
                }

                char? requiredLetter = RequiredLetter;
                if (requiredLetter.HasValue && char.ToLowerInvariant(correctedWord[0]) != char.ToLowerInvariant(requiredLetter.Value))
                {
                    State = GameState.GeminiWon;
                    return new MoveResult(MoveOutcome.WrongLetter);
                }

                if (History.Contains(correctedWord))
                {
                    State = GameState.GeminiWon;
                    return new MoveResult(MoveOutcome.AlreadyUsed);
                }

                var playerTurn = new Turn(History.Count + 1, Player.Human, correctedWord, originalWord, null);
                if (reply.Status == ReplyStatus.NoWordLeft)
                {
                    History.Add(playerTurn);
                    State = GameState.PlayerWon;
                    return new MoveResult(MoveOutcome.PlayerWins, playerTurn);
                }

                string? geminiWord = NormalizeReplyWord(reply.ReplyWord);
                if (geminiWord == null || !IsValidReplyWord(geminiWord, correctedWord))
                {
                    if (attempt == MaxRetries)
                    {
                        History.Add(playerTurn);
                        State = GameState.PlayerWon;
                        return new MoveResult(MoveOutcome.PlayerWins, playerTurn);
                    }

                    continue;
                }

                var geminiTurn = new Turn(History.Count + 2, Player.Gemini, geminiWord, geminiWord, reply.Explanation);
                History.Add(playerTurn);
                History.Add(geminiTurn);
                return new MoveResult(MoveOutcome.Accepted, playerTurn, geminiTurn);
            }

            throw new InvalidOperationException("The Gemini retry loop ended unexpectedly.");
        }

        public void NewGame()
        {
            History.Clear();
            State = GameState.InProgress;
        }

        private static bool IsWellFormed(string word)
        {
            if (string.IsNullOrEmpty(word))
            {
                return false;
            }

            foreach (char character in word)
            {
                if (!IsAsciiLetter(character))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool IsAsciiLetter(char character)
        {
            return (character >= 'a' && character <= 'z') || (character >= 'A' && character <= 'Z');
        }

        private static string NormalizeRequiredWord(string? word, string propertyName)
        {
            if (word == null || string.IsNullOrWhiteSpace(word))
            {
                throw new FormatException("Gemini reply is missing " + propertyName + ".");
            }

            string normalizedWord = word.Trim().ToLowerInvariant();
            if (!IsWellFormed(normalizedWord))
            {
                throw new FormatException("Gemini " + propertyName + " must contain letters only.");
            }

            return normalizedWord;
        }

        private static string? NormalizeReplyWord(string? word)
        {
            if (word == null)
            {
                return null;
            }

            string normalizedWord = word.Trim();
            return normalizedWord.Length == 0 ? null : normalizedWord.ToLowerInvariant();
        }

        private bool IsValidReplyWord(string replyWord, string correctedWord)
        {
            return IsWellFormed(replyWord) &&
                char.ToLowerInvariant(replyWord[0]) == char.ToLowerInvariant(correctedWord[correctedWord.Length - 1]) &&
                !History.Contains(replyWord) &&
                !string.Equals(replyWord, correctedWord, StringComparison.OrdinalIgnoreCase);
        }
    }
}
