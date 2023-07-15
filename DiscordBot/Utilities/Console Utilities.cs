using System;
using System.Collections.Generic;
using System.Threading;

namespace DiscordBot.Utilities;

public static class Utilities
{
    private static readonly Dictionary<char, ConsoleColor> Colors = new()
    {
        { 'g', ConsoleColor.Green },
        { 'b', ConsoleColor.Blue },
        { 'r', ConsoleColor.Red },
        { 'm', ConsoleColor.Magenta },
        { 'y', ConsoleColor.Yellow }
    };

    private static readonly char ColorPrefix = '&';


    private static bool CanAproximateTo(this float f, float y)
    {
        return MathF.Abs(f - y) < 0.000001;
    }


    /// <summary>
    ///     A way to create a table based on input data
    /// </summary>
    /// <param name="data">The List of arrays of strings that represent the rows.</param>
    public static void FormatAndAlignTable(List<string[]> data, TableFormat format)
    {
        if (format == TableFormat.CENTER_EACH_COLUMN_BASED)
        {
            var tableLine  = '-';
            var tableCross = '+';
            var tableWall  = '|';

            var len = new int[data[0].Length];
            foreach (var line in data)
                for (var i = 0; i < line.Length; i++)
                    if (line[i].Length > len[i])
                        len[i] = line[i].Length;


            foreach (var row in data)
            {
                if (row[0][0] == tableLine)
                    Console.Write(tableCross);
                else
                    Console.Write(tableWall);
                for (var l = 0; l < row.Length; l++)
                {
                    if (row[l][0] == tableLine)
                    {
                        for (var i = 0; i < len[l] + 4; ++i)
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
                        var lenHalf = row[l].Length / 2;
                        for (var i = 0; i < (len[l] + 4) / 2 - lenHalf; ++i)
                            Console.Write(" ");
                        Console.Write(row[l]);
                        for (var i = (len[l] + 4) / 2 + lenHalf + 1; i < len[l] + 4; ++i)
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
            var maxLen = 0;
            foreach (var row in data)
                foreach (var s in row)
                    if (s.Length > maxLen)
                        maxLen = s.Length;

            var div = (maxLen + 4) / 2;

            foreach (var row in data)
            {
                Console.Write("\t");
                if (row[0] == "-")
                    Console.Write("+");
                else
                    Console.Write("|");

                foreach (var s in row)
                {
                    if (s == "-")
                    {
                        for (var i = 0; i < maxLen + 4; ++i)
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
                        var lenHalf = s.Length / 2;
                        for (var i = 0; i < div - lenHalf; ++i)
                            Console.Write(" ");
                        Console.Write(s);
                        for (var i = div + lenHalf + 1; i < maxLen + 4; ++i)
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
            var widths                = new int[data[0].Length];
            var space_between_columns = 3;
            for (var i = 0; i < data.Count; i++)
                for (var j = 0; j < data[i].Length; j++)
                    if (data[i][j].Length > widths[j])
                        widths[j] = data[i][j].Length;

            for (var i = 0; i < data.Count; i++)
            {
                for (var j = 0; j < data[i].Length; j++)
                {
                    if (data[i][j] == "-")
                        data[i][j] = " ";
                    Console.Write(data[i][j]);
                    for (var k = 0; k < widths[j] - data[i][j].Length + 1 + space_between_columns; k++)
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
        var initialForeGround = Console.ForegroundColor;
        var input             = text.ToCharArray();
        for (var i = 0; i < input.Length; i++)
            if (input[i] == ColorPrefix)
            {
                if (i + 1 < input.Length)
                {
                    if (Colors.ContainsKey(input[i + 1]))
                    {
                        Console.ForegroundColor = Colors[input[i + 1]];
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
            {
                Console.Write(input[i]);
            }

        Console.ForegroundColor = initialForeGround;
        if (appendNewLineAtEnd)
            Console.WriteLine();
    }


    public class Spinner
    {
        private readonly string[] Sequence;
        private          bool     isRunning;
        private          int      position;
        private          Thread   thread;
        public string Message;

        public Spinner()
        {
            Sequence = new[] { "|", "/", "-", "\\" };
            position = 0;
        }

        public void Start()
        {
            Console.CursorVisible = false;
            isRunning             = true;
            thread = new Thread(() =>
            {
                while (isRunning)
                {
                    Console.SetCursorPosition(0, Console.CursorTop);
                    Console.Write(" " + Sequence[position] + " " + Message + "         ");
                    position++;
                    if (position >= Sequence.Length)
                        position = 0;
                    Thread.Sleep(100);
                }
            });

            thread.Start();
        }

        public void Stop()
        {
            isRunning             = false;
            Console.CursorVisible = true;
        }
    }
}
