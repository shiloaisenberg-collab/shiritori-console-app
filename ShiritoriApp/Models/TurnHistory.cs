#nullable enable
using System;
using System.Collections.Generic;

namespace ShiritoriApp
{
    internal sealed class TurnHistory
    {
        private readonly List<Turn> turns = new List<Turn>();

        public List<Turn> Turns => turns;
        public int Count => turns.Count;
        public Turn? LastTurn => turns.Count == 0 ? null : turns[turns.Count - 1];

        public void Add(Turn turn)
        {
            if (turn == null)
            {
                throw new ArgumentNullException(nameof(turn));
            }

            turns.Add(turn);
        }

        public bool Contains(string word)
        {
            if (string.IsNullOrWhiteSpace(word))
            {
                return false;
            }

            string normalizedWord = word.Trim();
            return turns.Exists(turn => string.Equals(turn.Word, normalizedWord, StringComparison.OrdinalIgnoreCase));
        }

        public List<string> UsedWords()
        {
            var words = new List<string>(turns.Count);
            foreach (Turn turn in turns)
            {
                words.Add(turn.Word.ToLowerInvariant());
            }

            return words;
        }

        public void Clear()
        {
            turns.Clear();
        }
    }
}
