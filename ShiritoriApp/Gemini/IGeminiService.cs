using System.Threading.Tasks;

namespace ShiritoriApp
{
    internal interface IGeminiService
    {
        Task<string> AskAsync(string prompt);
    }
}
