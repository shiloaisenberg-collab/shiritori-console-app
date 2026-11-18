#nullable enable

namespace ShiritoriApp
{
    public enum MoveOutcome
    {
        Accepted,
        NotWellFormed,
        NotAWord,
        WrongLetter,
        AlreadyUsed,
        PlayerWins,
    }

    internal sealed class MoveResult
    {
        public MoveResult(MoveOutcome outcome, Turn? playerTurn = null, Turn? geminiTurn = null)
        {
            Outcome = outcome;
            PlayerTurn = playerTurn;
            GeminiTurn = geminiTurn;
        }

        public MoveOutcome Outcome { get; }
        public Turn? PlayerTurn { get; }
        public Turn? GeminiTurn { get; }
    }
}
