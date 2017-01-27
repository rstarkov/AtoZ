using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace AtoZ
{
    class Program
    {
        static void Main()
        {
            var bestAnyOrder = TimeSpan.MaxValue;
            var bestAlphabetic = TimeSpan.MaxValue;

            while (true)
            {
                Console.WriteLine("Type every letter A-Z exactly once. Press Enter to start!");
                Console.ReadLine();

                Console.Clear();
                Console.WriteLine("Go!");
                var start = DateTime.UtcNow;
                var pressed = new Dictionary<char, TimeSpan>();
                while (true)
                {
                    var key = Console.ReadKey(true);
                    Console.Write((key.KeyChar + " ").ToUpper());

                    if (key.KeyChar < 'a' || key.KeyChar > 'z')
                        break;

                    if (pressed.ContainsKey(key.KeyChar))
                        break;
                    pressed[key.KeyChar] = DateTime.UtcNow - start;

                    if (pressed.Count == 26)
                        break;
                }

                Console.WriteLine('\x7');

                var wait = DateTime.UtcNow;
                while (DateTime.UtcNow < wait + TimeSpan.FromSeconds(1) && !Console.KeyAvailable && pressed.Count == 26)
                    Thread.Sleep(10);

                if (pressed.Count == 26 && !Console.KeyAvailable)
                {
                    var alphabetic = pressed.OrderBy(kvp => kvp.Key).SequenceEqual(pressed.OrderBy(kvp => kvp.Value));
                    var time = pressed.Values.Max() - pressed.Values.Min(); // optionally remove Min
                    Console.WriteLine($"Well done – {(alphabetic ? "ALPHABETIC" : "any order")} – {time.TotalMilliseconds:#,0} ms!");
                    if (bestAnyOrder > time)
                        bestAnyOrder = time;
                    if (alphabetic && bestAlphabetic > time)
                        bestAlphabetic = time;
                }
                else
                {
                    Console.WriteLine("OOPS!...");
                }

                Console.WriteLine();
                if (bestAnyOrder != TimeSpan.MaxValue)
                    Console.WriteLine($"Best any order: {bestAnyOrder.TotalMilliseconds:#,0} ms");
                if (bestAlphabetic != TimeSpan.MaxValue)
                    Console.WriteLine($"Best alphabetic: {bestAlphabetic.TotalMilliseconds:#,0} ms");

                Console.WriteLine();
            }
        }
    }
}
