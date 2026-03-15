using BLL.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Service
{
    public class ReplaceService
    {
        private readonly string LogFilePath = "log.txt";
        private string _copyDirectory;
        private List<string> _words;
        private CancellationToken _cancellationToken;
        private ManualResetEventSlim _manualResetEvent;
        private List<Log> logs;
        private Dictionary<string, int> _replacementsPerWord;

        public event Action FileProcessed;

        public ReplaceService(List<string> words, CancellationToken cancellationToken, ManualResetEventSlim manualResetEvent, string copyDirectory)
        {
            logs = new List<Log>();
            _words = new List<string>();
            _replacementsPerWord = new Dictionary<string, int>();
            foreach (string word in words)
            {
                _replacementsPerWord.Add(word, 0);
                _words.Add(word);
            }
            _cancellationToken = cancellationToken;
            _manualResetEvent = manualResetEvent;
            _copyDirectory = copyDirectory;
        }
        public async Task ReplaceBannedWords()
        {
            try
            {
                DriveInfo[] drives = DriveInfo.GetDrives();
                foreach (DriveInfo drive in drives)
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _manualResetEvent.Wait(_cancellationToken);
                    if (drive.Name.Equals(@"C:\", StringComparison.OrdinalIgnoreCase))
                        continue;

                    if (!drive.IsReady)
                    {
                        continue;
                    }

                    await Task.Run(() => SearchFiles(drive.RootDirectory.FullName));
                }
                LogReport();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void LogReport()
        {
            //MessageBox.Show("Done!");
            using (FileStream fileStream = new FileStream(LogFilePath, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(fileStream))
                {
                    foreach (var log in logs)
                    {
                        writer.WriteLine(log.ToString());
                    }
                    foreach (var item in _replacementsPerWord.OrderByDescending(r => r.Value).Take(10))
                    {
                        writer.WriteLine($"{item.Key}: {item.Value}");
                    }

                }
            }
        }

        private async Task SearchFiles(string fullName)
        {
            try
            {
                foreach (var direct in Directory.EnumerateDirectories(fullName))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _manualResetEvent.Wait(_cancellationToken);
                    await Task.Run(() => SearchFiles(direct));
                }

                foreach (var file in Directory.EnumerateFiles(fullName))
                {
                    _cancellationToken.ThrowIfCancellationRequested();
                    _manualResetEvent.Wait(_cancellationToken);
                    await LookIntoFiles(file);
                }
            }
            catch (UnauthorizedAccessException)
            {

            }
            catch (PathTooLongException)
            {

            }
        }


        private async Task LookIntoFiles(string file)
        {
            string type = Path.GetExtension(file);
            if (type == ".txt")
            {
                using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
                {
                    using (StreamReader reader = new StreamReader(fileStream))
                    {
                        FileProcessed?.Invoke();

                        while (reader.Peek() > 0)
                        {
                            string line = await reader.ReadLineAsync();
                            string[] words = line.Split(' ');
                            _cancellationToken.ThrowIfCancellationRequested();
                            _manualResetEvent.Wait(_cancellationToken);
                            foreach (string word in words)
                            {
                                foreach (string banned in _words)
                                {
                                    if (banned.Equals(word, StringComparison.OrdinalIgnoreCase))
                                    {
                                        await CopyToFile(file);
                                        return;
                                    }
                                }
                            }
                        }
                    }

                }
            }

        }

        private async Task CopyToFile(string file)
        {
            string name = Path.GetFileName(file);
            string newFilePath = Path.Combine(_copyDirectory, name);
            FileInfo fileInfo = new FileInfo(file);
            logs.Add(new Log() { FilePath = Path.GetFullPath(file), ReplaceAmount = 0, Size = Convert.ToInt32(fileInfo.Length / 1024.0) });
            string fileText = "";
            using (FileStream fileStream = new FileStream(file, FileMode.Open, FileAccess.Read))
            {
                using (StreamReader reader = new StreamReader(fileStream))
                {
                    while (reader.Peek() > 0)
                    {
                        string line = await reader.ReadLineAsync();
                        _cancellationToken.ThrowIfCancellationRequested();
                        _manualResetEvent.Wait(_cancellationToken);
                        fileText += line + "\n";
                    }
                }
            }

            using (var writer = new StreamWriter(newFilePath, false))
            {
                foreach (var line in fileText.Split('\n'))
                {
                    string newLine = line;
                    foreach (var word in line.Split(' '))
                    {
                        foreach (string banned in _words)
                        {
                            if (banned.Equals(word, StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******");
                            }
                            else if (banned.Equals(word.Split(',')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******,");
                            }
                            else if (banned.Equals(word.Split(':')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******:");
                            }
                            else if (banned.Equals(word.Split(';')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******;");
                            }
                            else if (banned.Equals(word.Split('/')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******/");
                            }
                            else if (banned.Equals(word.Split('\'')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******'");
                            }
                            else if (banned.Equals(word.Split('"')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******\"");
                            }
                            else if (banned.Equals(word.Split('*')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "********");
                            }
                            else if (banned.Equals(word.Split('&')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******&");
                            }
                            else if (banned.Equals(word.Split('$')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******$");
                            }
                            else if (banned.Equals(word.Split('@')[0], StringComparison.OrdinalIgnoreCase))
                            {
                                _replacementsPerWord[banned]++;
                                logs[logs.Count - 1].ReplaceAmount++;
                                newLine = newLine.Replace(word, "*******@");
                            }

                        }

                    }
                    await writer.WriteLineAsync(newLine.ToString());
                }
            }
            }
        }
}
