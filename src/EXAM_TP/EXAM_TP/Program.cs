using BLL.Models;
using BLL.Service;
namespace EXAM_TP
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            List<string> words = new List<string>();
            CancellationTokenSource cancellationToken = new CancellationTokenSource();
            ManualResetEventSlim manualResetEvent = new ManualResetEventSlim(true);
            Console.Write("Input banned words ");
            string line = Console.ReadLine();
            foreach (string word in line.Split(' '))
            {
                words.Add(word.Trim(',', '.', ';', ':', '/', '!', '?', '&'));

            }
            string copyDirectory = "ReplaceFolder";
            ReplaceService _service = new ReplaceService(words, cancellationToken.Token, manualResetEvent, copyDirectory);
            await _service.ReplaceBannedWords();
        }
    }
}
