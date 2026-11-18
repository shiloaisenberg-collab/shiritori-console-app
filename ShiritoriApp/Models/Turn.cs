#nullable enable
using System;

namespace ShiritoriApp
{
    public enum Player
    {
        Human,
        Gemini,
    }

    internal sealed class Turn
    {
        public Turn(int number, Player player, string word, string originalWord, string? explanation)
        {
            if (number < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(number), "Turn numbers start at 1.");
            }

            if (!Enum.IsDefined(typeof(Player), player))
            {
                throw new ArgumentOutOfRangeException(nameof(player));
            }

            if (word == null)
            {
                throw new ArgumentNullException(nameof(word));
            }

            Word = word.Trim().ToLowerInvariant();
            if (Word.Length == 0)
            {
                throw new ArgumentException("A turn must contain a word.", nameof(word));
            }

            Number = number;
            Player = player;
            OriginalWord = string.IsNullOrWhiteSpace(originalWord) ? Word : originalWord;
            Explanation = explanation;
            WasCorrected = !string.Equals(OriginalWord, Word, StringComparison.OrdinalIgnoreCase);
            LastLetter = Word[Word.Length - 1];
        }

        public int Number { get; }
        public Player Player { get; }
        public string Word { get; }
        public string OriginalWord { get; }
        public string? Explanation { get; }
        public bool WasCorrected { get; }
        public char LastLetter { get; }
    }
}
