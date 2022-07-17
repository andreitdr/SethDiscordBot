using Discord;
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
                        Console.Write(MathF.Round(progress, 2) + " %       ");
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
        public static void FormatAndAlignTable(List<string[]> data, TableFormat format = TableFormat.CENTER_EACH_COLUMN_BASED)
        {
            if (format == TableFormat.CENTER_EACH_COLUMN_BASED)
            {
                char tableLine  = '-';
                char tableCross = '+';
                char tableWall  = '|';

                int[] len = new int[data[0].Length];
                foreach (var line in data)
                    for (int i = 0; i < line.Length; i++)
                        if (line[i].Length > len[i])
                            len[i] = line[i].Length;


                foreach (string[] row in data)
                {
                    if (row[0][0] == tableLine)
                        Console.Write(tableCross);
                    else
                        Console.Write(tableWall);
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

                return;
            }

            if (format == TableFormat.CENTER_OVERALL_LENGTH)
            {
                int maxLen = 0;
                foreach (string[] row in data)
                    foreach (string s in row)
                        if (s.Length > maxLen)
                            maxLen = s.Length;

                int div = (maxLen + 4) / 2;

                foreach (string[] row in data)
                {
                    Console.Write("\t");
                    if (row[0] == "-")
                        Console.Write("+");
                    else
                        Console.Write("|");

                    foreach (string s in row)
                    {
                        if (s == "-")
                        {
                            for (int i = 0; i < maxLen + 4; ++i)
                                Console.Write("-");
                        }
                        else if (s.Length == maxLen)
                        {
                            Console.Write("  ");
                            Console.Write(s);
                            Console.Write("  ");
                        }
                        else
                        {
                            int lenHalf = s.Length / 2;
                            for (int i = 0; i < div - lenHalf; ++i)
                                Console.Write(" ");
                            Console.Write(s);
                            for (int i = div + lenHalf + 1; i < maxLen + 4; ++i)
                                Console.Write(" ");
                            if (s.Length % 2 == 0)
                                Console.Write(" ");
                        }

                        if (s == "-")
                            Console.Write("+");
                        else
                            Console.Write("|");
                    }

                    Console.WriteLine(); //end line
                }

                return;
            }

            if (format == TableFormat.DEFAULT)
            {
                int[] widths                = new int[data[0].Length];
                int   space_between_columns = 5;
                for (int i = 0; i < data.Count; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        if (data[i][j].Length > widths[j])
                            widths[j] = data[i][j].Length;
                    }
                }

                for (int i = 0; i < data.Count; i++)
                {
                    for (int j = 0; j < data[i].Length; j++)
                    {
                        if (data[i][j] == "-")
                            data[i][j] = " ";
                        Console.Write(data[i][j]);
                        for (int k = 0; k < widths[j] - data[i][j].Length + 1 + space_between_columns; k++)
                            Console.Write(" ");
                    }

                    Console.WriteLine();
                }

                return;
            }

            throw new Exception("Unknown type of table");
        }

        public static void WriteColorText(string text, bool appendNewLineAtEnd = true)
        {
            ConsoleColor initialForeGround = Console.ForegroundColor;
            Dictionary<char, ConsoleColor> colors = new()
            {
                { 'g', ConsoleColor.Green },
                { 'b', ConsoleColor.Blue },
                { 'r', ConsoleColor.Red },
                { 'm', ConsoleColor.Magenta },
                { 'y', ConsoleColor.Yellow },
                { 'c', initialForeGround }
            };

            char[] input = text.ToCharArray();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '&')
                {
                    if (i + 1 < input.Length)
                    {
                        if (colors.ContainsKey(input[i + 1]))
                        {
                            Console.ForegroundColor = colors[input[i + 1]];
                            i++;
                        }
                    }
                }
                else
                    Console.Write(input[i]);
            }

            Console.ForegroundColor = initialForeGround;
            if (appendNewLineAtEnd)
                Console.WriteLine();
        }
    }
}
