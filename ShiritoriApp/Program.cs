#nullable enable
using System;
using System.Net.Http;
using System.Threading.Tasks;

namespace ShiritoriApp
{
    internal static class Program
    {
        private const string ModelName = "gemini-3.6-flash";

        private static async Task Main(string[] args)
        {
            string? apiKey = Environment.GetEnvironmentVariable("GEMINI_API_KEY");
            if (string.IsNullOrWhiteSpace(apiKey))
            {
                Console.Error.WriteLine("GEMINI_API_KEY environment variable is not set.");
                return;
            }

            var game = new Shiritori(new GeminiService(apiKey, ModelName));
            Console.WriteLine("Shiritori: enter a word, /new, /history, or /quit.");

            while (true)
            {
                if (game.State == GameState.InProgress)
                {
                    string required = game.RequiredLetter.HasValue
                        ? "starting with " + game.RequiredLetter.Value
                        : "any letter";
                    Console.Write("Your word (" + required + "): ");
                }
                else
                {
                    Console.Write("Game over. Enter /new, /history, or /quit: ");
                }

                string? input = Console.ReadLine();
                if (input == null)
                {
                    break;
                }

                string command = input.Trim();
                if (string.Equals(command, "/quit", StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                if (string.Equals(command, "/new", StringComparison.OrdinalIgnoreCase))
                {
                    game.NewGame();
                    Console.WriteLine("New game started.");
                    continue;
                }

                if (string.Equals(command, "/history", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHistory(game);
                    continue;
                }

                if (game.State != GameState.InProgress)
                {
                    Console.WriteLine("The game is over. Choose /new, /history, or /quit.");
                    continue;
                }

                MoveResult result;
                try
                {
                    result = await game.PlayAsync(input);
                }
                catch (HttpRequestException)
                {
                    Console.WriteLine("Could not reach Gemini. Try the same word again.");
                    continue;
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("Gemini took too long. Try the same word again.");
                    continue;
                }
                catch (FormatException)
                {
                    Console.WriteLine("Gemini sent a reply I couldn't read. Try the same word again.");
                    continue;
                }
                catch (Exception exception)
                {
                    Console.WriteLine("Gemini request failed: " + exception.Message);
                    continue;
                }

                PrintMoveResult(result);
                if (game.State != GameState.InProgress)
                {
                    PrintGameOver(game);
                }
            }
        }

        private static void PrintMoveResult(MoveResult result)
        {
            switch (result.Outcome)
            {
                case MoveOutcome.NotWellFormed:
                    Console.WriteLine("Please type one word, letters only.");
                    break;
                case MoveOutcome.NotAWord:
                    Console.WriteLine("That isn't a recognized English word.");
                    break;
                case MoveOutcome.WrongLetter:
                    Console.WriteLine("Your word doesn't start with the required letter.");
                    break;
                case MoveOutcome.AlreadyUsed:
                    Console.WriteLine("That word has already been used.");
                    break;
                case MoveOutcome.PlayerWins:
                    Console.WriteLine("Gemini has no valid word left. You win!");
                    break;
                case MoveOutcome.Accepted:
                    PrintPlayerTurn(result.PlayerTurn);
                    PrintGeminiTurn(result.GeminiTurn);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(result), "Unknown move outcome.");
            }
        }

        private static void PrintPlayerTurn(Turn? turn)
        {
            if (turn != null && turn.WasCorrected)
            {
                Console.WriteLine("(You typed " + turn.OriginalWord + ", counted as " + turn.Word + ".)");
            }
        }

        private static void PrintGeminiTurn(Turn? turn)
        {
            if (turn == null)
            {
                return;
            }

            Console.WriteLine("Gemini: " + turn.Word);
            if (!string.IsNullOrWhiteSpace(turn.Explanation))
            {
                Console.WriteLine(turn.Explanation);
            }
        }

        private static void PrintHistory(Shiritori game)
        {
            if (game.History.Count == 0)
            {
                Console.WriteLine("No words have been played.");
                return;
            }

            foreach (Turn turn in game.History.Turns)
            {
                string player = turn.Player == Player.Human ? "You" : "Gemini";
                string correction = turn.WasCorrected ? " (typed " + turn.OriginalWord + ")" : string.Empty;
                Console.WriteLine(turn.Number + ". " + player + ": " + turn.Word + correction);
                if (!string.IsNullOrWhiteSpace(turn.Explanation))
                {
                    Console.WriteLine("   " + turn.Explanation);
                }
            }
        }

        private static void PrintGameOver(Shiritori game)
        {
            string winner = game.State == GameState.PlayerWon ? "You win!" : "Gemini wins.";
            Console.WriteLine(winner + " Words played: " + game.History.Count + ".");
        }
    }
}
