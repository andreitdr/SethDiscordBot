using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace DiscordBot.Utilities;

public class TableData
{
    public List<string>   Columns;
    public List<string[]> Rows;

    public bool IsEmpty => Rows.Count == 0;
    public bool HasRoundBorders { get; set; } = true;

    public TableData(List<string> columns)
    {
        Columns = columns;
        Rows    = new List<string[]>();
    }

    public TableData(string[] columns)
    {
        Columns = columns.ToList();
        Rows    = new List<string[]>();
    }

    public void AddRow(string[] row)
    {
        Rows.Add(row);
    }
}

public static class ConsoleUtilities
{

    public static async Task<T> ExecuteWithProgressBar<T>(Task<T> function, string message)
    {
        T result = default;
        await AnsiConsole.Progress()
                         .Columns(
                             new ProgressColumn[]
                             {
                                 new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn()
                             }
                         )
                         .StartAsync(
                             async ctx =>
                             {
                                 var task = ctx.AddTask(message);
                                 task.IsIndeterminate = true;
                                 result               = await function;
                                 task.Increment(100);

                             }
                         );

        return result;
    }

    public static async Task ExecuteWithProgressBar(Task function, string message)
    {
        await AnsiConsole.Progress()
                         .Columns(new ProgressColumn[]
                             {
                                 new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn()
                             }
                         )
                         .StartAsync(async ctx =>
                             {
                                 var task = ctx.AddTask(message);
                                 task.IsIndeterminate = true;
                                 await function;
                                 task.Increment(100);

                             }
                         );
    }

    private static readonly Dictionary<char, ConsoleColor> Colors = new()
    {
        {
            'g', ConsoleColor.Green
        },
        {
            'b', ConsoleColor.Blue
        },
        {
            'r', ConsoleColor.Red
        },
        {
            'm', ConsoleColor.Magenta
        },
        {
            'y', ConsoleColor.Yellow
        }
    };

    private static readonly char ColorPrefix = '&';

    private static bool CanAproximateTo(this float f, float y)
    {
        return MathF.Abs(f - y) < 0.000001;
    }

    public static void PrintAsTable(this TableData tableData)
    {
        var table = new Table();
        table.Border(tableData.HasRoundBorders ? TableBorder.Rounded : TableBorder.Square);
        table.AddColumns(tableData.Columns.ToArray());
        foreach (var row in tableData.Rows)
            table.AddRow(row);

        AnsiConsole.Write(table);
    }


    /// <summary>
    ///     A way to create a table based on input data
    /// </summary>
    /// <param name="data">The List of arrays of string that represent the rows.</param>
    public static void FormatAndAlignTable(List<string[]> data, TableFormat format)
    {
        if (format == TableFormat.SPECTRE_CONSOLE)
        {
            var table = new Table();
            table.Border(TableBorder.Rounded);
            table.AddColumns(data[0]);
            data.RemoveAt(0);
            foreach (var row in data)
                table.AddRow(row);

            AnsiConsole.Write(table);

            return;
        }

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
        public           string   Message;
        private          int      position;
        private          Thread   thread;

        public Spinner()
        {
            Sequence = new[]
            {
                "|", "/", "-", "\\"
            };
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
                }
            );

            thread.Start();
        }

        public void Stop()
        {
            isRunning             = false;
            Console.CursorVisible = true;
        }
    }
}
