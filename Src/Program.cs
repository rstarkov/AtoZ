using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using RT.Util;
using RT.Util.Collections;
using RT.Util.Consoles;
using RT.Util.ExtensionMethods;
using RT.Util.Text;

namespace AtoZ
{
    class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WindowWidth = 200;
            Console.WindowHeight = 40;

            var curUser = Environment.UserName;
            curUser = curUser.Substring(0, 1).ToUpper() + curUser.Substring(1).ToLower();
            Settings settings;
            SettingsUtil.LoadSettings(out settings);
            var highscore = settings.Highscores[curUser];

            var recent = new AutoDictionary<string, Highscore>(_ => new Highscore());
            int cur = 0;
            ConsoleColoredString lastAttemptOutcome = null;

            while (true)
            {
                Console.Clear();
                Console.WriteLine("Current highscores, in milliseconds:");
                WriteToConsole(settings.Highscores.Concat(recent).ToDictionary());

                if (lastAttemptOutcome != null)
                {
                    Console.WriteLine();
                    ConsoleUtil.WriteLine("Last attempt: " + lastAttemptOutcome);
                }

                Console.WriteLine();
                Console.WriteLine("Type every letter A-Z exactly once, either alphabetically or in any order. Wait 1 second when done, or press space to restart.");
                Console.WriteLine();

                var pressed = new Dictionary<char, DateTime>();
                Console.Title = string.Join(" ", Enumerable.Range('A', 26).Select(x => (char) x).Except(pressed.Keys).OrderBy(x => x));
                while (true)
                {
                    var key = Console.ReadKey(true);
                    Console.Write((key.KeyChar + " ").ToUpper());

                    if (key.KeyChar < 'a' || key.KeyChar > 'z')
                        break;
                    var chr = char.ToUpper(key.KeyChar);

                    if (pressed.ContainsKey(chr))
                        break;
                    pressed[chr] = DateTime.UtcNow;
                    Console.Title = string.Join(" ", Enumerable.Range('A', 26).Select(x => (char) x).Except(pressed.Keys).OrderBy(x => x));

                    if (pressed.Count == 26)
                        break;
                }

                Console.WriteLine();
                Console.WriteLine('\x7');

                if (pressed.Count == 26)
                {
                    Console.WriteLine("Don't press anything now, to confirm you've typed just the 26 letters accurately and nothing else!");
                    var wait = DateTime.UtcNow;
                    while (DateTime.UtcNow < wait + TimeSpan.FromSeconds(1) && !Console.KeyAvailable)
                        Thread.Sleep(10);
                }

                if (pressed.Count == 26 && !Console.KeyAvailable)
                {
                    UpdateHighscore(highscore, pressed);
                    cur++;
                    UpdateHighscore(recent[$"Recent: {cur:00}"], pressed);
                    if (recent.ContainsKey($"Recent: {cur - 20:00}"))
                        recent.Remove($"Recent: {cur - 20:00}");

                    lastAttemptOutcome = "";
                    char expected = 'A';
                    bool alphabetic = true;
                    foreach (var kvp in pressed.OrderBy(kvp => kvp.Value))
                    {
                        if (kvp.Key != expected)
                            alphabetic = false;
                        lastAttemptOutcome += $"{kvp.Key} ".Color(kvp.Key != expected ? ConsoleColor.Red : alphabetic ? ConsoleColor.Green : ConsoleColor.White);
                        expected = (char) (kvp.Key + 1);
                    }
                    lastAttemptOutcome = (alphabetic ? "ALPHABETIC" : "any order") + " " + lastAttemptOutcome;

                    settings.Save();
                }
                else
                {
                    lastAttemptOutcome = "OOPS!...".Color(ConsoleColor.Magenta);
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }

        private static void UpdateHighscore(Highscore highscore, Dictionary<char, DateTime> pressed)
        {
            highscore.BestAnyOrder = minTime(highscore.BestAnyOrder, pressed.Values.Max() - pressed.Values.Min());

            var orderedResults = pressed.OrderBy(kvp => kvp.Value).ToList();
            for (int i = 0; i < 26; i++)
            {
                if (orderedResults[i].Key != (char) ('A' + i))
                    break;
                highscore.BestAlphabetic[orderedResults[i].Key] = minTime(highscore.BestAlphabetic[orderedResults[i].Key], orderedResults[i].Value - orderedResults[0].Value);
            }
        }

        private static void WriteToConsole(IDictionary<string, Highscore> highscores)
        {
            var table = new TextTable { ColumnSpacing = 2, DefaultAlignment = HorizontalTextAlignment.Right };
            int row = 2;
            foreach (var hs in highscores.OrderBy(kvp => kvp.Key.StartsWith("Recent") ? 1 : 0).ThenBy(kvp => kvp.Key))
            {
                table.SetCell(0, 0, "Name");
                table.SetCell(1, 0, "Any order");
                table.SetCell(2, 0, "Alphabetic", colSpan: 26);

                var best = highscores.Values.Min(h => h.BestAnyOrder);
                table.SetCell(0, row, hs.Key);
                table.SetCell(1, row, toDisplayStr(hs.Value.BestAnyOrder, best));
                for (int i = 0; i < 26; i++)
                {
                    var curChar = (char) ('A' + i);
                    best = highscores.Values.Min(h => h.BestAlphabetic[curChar]);
                    table.SetCell(2 + i, 1, curChar.ToString());
                    table.SetCell(2 + i, row, toDisplayStr(hs.Value.BestAlphabetic[curChar], best));
                }
                row++;
            }
            table.WriteToConsole();
        }

        private static TimeSpan minTime(TimeSpan t1, TimeSpan t2)
        {
            return t1 < t2 ? t1 : t2;
        }

        private static ConsoleColoredString toDisplayStr(TimeSpan time, TimeSpan best)
        {
            var s = time == TimeSpan.MaxValue ? "∞" : $"{time.TotalMilliseconds:#,0}";
            return s.Color(time == best ? ConsoleColor.Green : ConsoleColor.Gray);
        }
    }

    [Settings("AtoZ", SettingsKind.Global)]
    class Settings : SettingsBase
    {
        public AutoDictionary<string, Highscore> Highscores = new AutoDictionary<string, Highscore>(_ => new Highscore());
    }

    class Highscore
    {
        public TimeSpan BestAnyOrder = TimeSpan.MaxValue;
        public AutoDictionary<char, TimeSpan> BestAlphabetic = new AutoDictionary<char, TimeSpan>(_ => TimeSpan.MaxValue);
    }
}
