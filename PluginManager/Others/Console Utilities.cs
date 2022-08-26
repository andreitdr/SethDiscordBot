using Discord;

using System;
using System.Collections.Generic;

namespace PluginManager.Others
{
    public static class Console_Utilities
    {
        public static void Initialize()
        {
            if (!Config.ContainsKey("TableVariables"))
                Config.AddValueToVariables("TableVariables", new Dictionary<string, string> { { "DefaultSpace", "3" } }, false);
            if (!Config.ContainsKey("ColorDataBase"))
                Config.AddValueToVariables("ColorDataBase", new Dictionary<char, ConsoleColor>()
                    {
                        { 'g', ConsoleColor.Green },
                        { 'b', ConsoleColor.Blue },
                        { 'r', ConsoleColor.Red },
                        { 'm', ConsoleColor.Magenta },
                        { 'y', ConsoleColor.Yellow },
                    }, false
                );

            if (!Config.ContainsKey("ColorPrefix"))
                Config.AddValueToVariables("ColorPrefix", '&', false);
        }


        /// <summary>
        /// Progress bar object
        /// </summary>
        public class ProgressBar
        {
            public ProgressBar(ProgressBarType type)
            {
                this.type = type;
            }

            public float Max { get; init; }
            public ConsoleColor Color { get; init; }
            public bool NoColor { get; init; }
            public ProgressBarType type { get; set; }

            private int BarLength = 32;
            private int position = 1;
            private bool positive = true;

            public void Update(float progress)
            {
                switch (type)
                {
                    case ProgressBarType.NORMAL:
                        UpdateNormal(progress);
                        return;
                    case ProgressBarType.NO_END:
                        if (progress <= 99.9f)
                            UpdateNoEnd();
                        return;
                    default:
                        return;
                }
            }

            private void UpdateNoEnd()
            {
                Console.CursorLeft = 0;
                Console.Write("[");
                for (int i = 1; i <= position; i++)
                    Console.Write(" ");
                Console.Write("<==()==>");
                position += positive ? 1 : -1;
                for (int i = position; i <= BarLength - 1 - (positive ? 0 : 2); i++)
                    Console.Write(" ");
                Console.Write("]");


                if (position == BarLength - 1 || position == 1)
                    positive = !positive;
            }

            private void UpdateNormal(float progress)
            {
                Console.CursorLeft = 0;
                Console.Write("[");
                Console.CursorLeft = BarLength;
                Console.Write("]");
                Console.CursorLeft = 1;
                float onechunk = 30.0f / Max;

                int position = 1;

                for (int i = 0; i < onechunk * progress; i++)
                {
                    Console.BackgroundColor = NoColor ? ConsoleColor.Black : this.Color;
                    Console.CursorLeft = position++;
                    Console.Write("#");
                }

                for (int i = position; i < BarLength; i++)
                {
                    Console.BackgroundColor = NoColor ? ConsoleColor.Black : ConsoleColor.DarkGray;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                Console.CursorLeft = BarLength + 4;
                Console.BackgroundColor = ConsoleColor.Black;
                if (progress.CanAproximateTo(Max))
                    Console.Write(progress + " %      ✓");
                else
                    Console.Write(MathF.Round(progress, 2) + " %       ");
            }
        }


        private static bool CanAproximateTo(this float f, float y) => (MathF.Abs(f - y) < 0.000001);


        /// <summary>
        /// A way to create a table based on input data
        /// </summary>
        /// <param name="data">The List of arrays of strings that represent the rows.</param>
        public static void FormatAndAlignTable(List<string[]> data, TableFormat format)
        {
            if (format == TableFormat.CENTER_EACH_COLUMN_BASED)
            {
                char tableLine = '-';
                char tableCross = '+';
                char tableWall = '|';

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
                int[] widths = new int[data[0].Length];
                int space_between_columns = int.Parse(Config.GetValue<Dictionary<string, string>>("TableVariables")?["DefaultSpace"]!);
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
            char[] input = text.ToCharArray();
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == Config.GetValue<char>("ColorPrefix"))
                {
                    if (i + 1 < input.Length)
                    {
                        if (Config.GetValue<Dictionary<char, ConsoleColor>>("ColorDataBase")!.ContainsKey(input[i + 1]))
                        {
                            Console.ForegroundColor = Config.GetValue<Dictionary<char, ConsoleColor>>("ColorDataBase")![input[i + 1]];
                            i++;
                        }
                        else if (input[i + 1] == 'c')
                        {
                            Console.ForegroundColor = initialForeGround;
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
