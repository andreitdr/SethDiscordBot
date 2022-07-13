using System;
using System.Collections.Generic;
using System.Linq;

namespace PluginManager.Others
{
    public static class Console_Utilities
    {
        /// <summary>
        /// Progress bar object
        /// </summary>
        public class ProgressBar
        {
            public float        Max     { get; init; }
            public ConsoleColor Color   { get; init; }
            public bool         NoColor { get; init; }


            public void Update(float progress, double speed = -1, string? unit = null)
            {
                Console.CursorLeft = 0;
                Console.Write("[");
                Console.CursorLeft = 32;
                Console.Write("]");
                Console.CursorLeft = 1;
                float onechunk = 30.0f / Max;

                int position = 1;

                for (int i = 0; i < onechunk * progress; i++)
                {
                    Console.BackgroundColor = NoColor ? ConsoleColor.Black : this.Color;
                    Console.CursorLeft      = position++;
                    Console.Write("#");
                }

                for (int i = position; i <= 31; i++)
                {
                    Console.BackgroundColor = NoColor ? ConsoleColor.Black : ConsoleColor.DarkGray;
                    Console.CursorLeft      = position++;
                    Console.Write(" ");
                }

                Console.CursorLeft = 35;
                Console.BackgroundColor = ConsoleColor.Black;
                if (speed is -1 || unit == null)
                {
                    if (progress.CanAproximateTo(Max))
                        Console.Write(progress + " %      ✓");
                    else
                        Console.Write(progress + " %       ");
                }
                else
                    Console.Write(progress + $"{speed} {unit}/s        ");
            }
        }


        private static bool CanAproximateTo(this float f, float y) => (MathF.Abs(f - y) < 0.000001);


        /// <summary>
        /// A way to create a table based on input data
        /// </summary>
        /// <param name="data">The List of arrays of strings that represent the rows.</param>
        public static void FormatAndAlignTable(List<string[]> data)
        {
            char tableLine  = '-';
            char tableCross = '+';
            char tableWall  = '|';

            int[] len = new int[data[0].Length];
            foreach (var line in data)
            {
                for (int i = 0; i < line.Length; i++)
                    if (line[i].Length > len[i])
                        len[i] = line[i].Length;
            }


            foreach (string[] row in data)
            {
                if (row[0][0] == tableLine) Console.Write(tableCross);
                else Console.Write(tableWall);
                for (int l = 0; l < row.Length; l++)
                {
                    if (row[l][0] == tableLine)
                    {
                        for (int i = 0; i < len[l] + 4; ++i)
                            Console.Write(tableLine);
                    }
                    else if (row[l].Length == len[l])
                    {
                        Console.Write("  ");
                        Console.Write(row[l]);
                        Console.Write("  ");
                    }
                    else
                    {

                        int lenHalf = row[l].Length / 2;
                        for (int i = 0; i < ((len[l] + 4) / 2 - lenHalf); ++i)
                            Console.Write(" ");
                        Console.Write(row[l]);
                        for (int i = (len[l] + 4) / 2 + lenHalf + 1; i < len[l] + 4; ++i)
                            Console.Write(" ");
                        if (row[l].Length % 2 == 0)
                            Console.Write(" ");
                    }

                    Console.Write(row[l][0] == tableLine ? tableCross : tableWall);
                }
                Console.WriteLine(); //end line

            }
        }

        public static void WriteColorText(string text, bool appendNewLine = true)
        {

            string[] words = text.Split(' ');
            ConsoleColor fg = Console.ForegroundColor;
            Dictionary<string, ConsoleColor> colors = new Dictionary<string, ConsoleColor>()
            {
                { "&g", ConsoleColor.Green },
                { "&b", ConsoleColor.Blue },
                { "&r", ConsoleColor.Red },
                { "&m", ConsoleColor.Magenta },
                { "&y", ConsoleColor.Yellow },
                { "&c", fg }
            };
            foreach (string word in words)
            {
                if (word.Length >= 2)
                {
                    string prefix = word.Substring(0, 2);
                    if (colors.ContainsKey(prefix))
                        Console.ForegroundColor = colors[prefix];
                }

                string m = colors.Keys.Aggregate(word, (current, key) => current.Replace(key, ""));

                Console.Write(m + " ");
            }

            Console.CursorLeft--;

            if (appendNewLine)
                Console.Write('\n');

            Console.ForegroundColor = fg;
        }

    }
}
