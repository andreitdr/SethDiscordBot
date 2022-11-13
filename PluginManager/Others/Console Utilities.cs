using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginManager.Others;

public static class Utilities
{
    private static Dictionary<char, ConsoleColor> Colors = new()
    {
                    { 'g', ConsoleColor.Green },
                    { 'b', ConsoleColor.Blue },
                    { 'r', ConsoleColor.Red },
                    { 'm', ConsoleColor.Magenta },
                    { 'y', ConsoleColor.Yellow }
    };

    private static char ColorPrefix = '&';


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
            var tableLine = '-';
            var tableCross = '+';
            var tableWall = '|';

            var len = new int[data[0].Length];
            foreach (var line in data)
                for (var i = 0; i < line.Length; i++)
                    if (line[i].Length > len[i])
                        len[i] = line[i].Length;


            foreach (var row in data)
            {
                if (row[0][0] == tableLine)
                    Logger.Write(tableCross);
                else
                    Logger.Write(tableWall);
                for (var l = 0; l < row.Length; l++)
                {
                    if (row[l][0] == tableLine)
                    {
                        for (var i = 0; i < len[l] + 4; ++i)
                            Logger.Write(tableLine);
                    }
                    else if (row[l].Length == len[l])
                    {
                        Logger.Write("  ");
                        Logger.Write(row[l]);
                        Logger.Write("  ");
                    }
                    else
                    {
                        var lenHalf = row[l].Length / 2;
                        for (var i = 0; i < (len[l] + 4) / 2 - lenHalf; ++i)
                            Logger.Write(" ");
                        Logger.Write(row[l]);
                        for (var i = (len[l] + 4) / 2 + lenHalf + 1; i < len[l] + 4; ++i)
                            Logger.Write(" ");
                        if (row[l].Length % 2 == 0)
                            Logger.Write(" ");
                    }

                    Logger.Write(row[l][0] == tableLine ? tableCross : tableWall);
                }

                Logger.WriteLine(); //end line
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
                Logger.Write("\t");
                if (row[0] == "-")
                    Logger.Write("+");
                else
                    Logger.Write("|");

                foreach (var s in row)
                {
                    if (s == "-")
                    {
                        for (var i = 0; i < maxLen + 4; ++i)
                            Logger.Write("-");
                    }
                    else if (s.Length == maxLen)
                    {
                        Logger.Write("  ");
                        Logger.Write(s);
                        Logger.Write("  ");
                    }
                    else
                    {
                        var lenHalf = s.Length / 2;
                        for (var i = 0; i < div - lenHalf; ++i)
                            Logger.Write(" ");
                        Logger.Write(s);
                        for (var i = div + lenHalf + 1; i < maxLen + 4; ++i)
                            Logger.Write(" ");
                        if (s.Length % 2 == 0)
                            Logger.Write(" ");
                    }

                    if (s == "-")
                        Logger.Write("+");
                    else
                        Logger.Write("|");
                }

                Logger.WriteLine(); //end line
            }

            return;
        }

        if (format == TableFormat.DEFAULT)
        {
            var widths = new int[data[0].Length];
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
                    Logger.Write(data[i][j]);
                    for (var k = 0; k < widths[j] - data[i][j].Length + 1 + space_between_columns; k++)
                        Logger.Write(" ");
                }

                Logger.WriteLine();
            }

            return;
        }

        throw new Exception("Unknown type of table");
    }

    public static void WriteColorText(string text, bool appendNewLineAtEnd = true)
    {
        if (!Logger.isConsole)
        {
            foreach (var item in Colors)
                text = text.Replace($"{ColorPrefix}{item.Key}", "").Replace("&c", "");
            Logger.Write(text);
            if (appendNewLineAtEnd)
                Logger.WriteLine();
            return;

        }
        var initialForeGround = Console.ForegroundColor;
        var input = text.ToCharArray();
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
                Logger.Write(input[i]);
            }

        Console.ForegroundColor = initialForeGround;
        if (appendNewLineAtEnd)
            Logger.WriteLine();
    }


    /// <summary>
    ///     Progress bar object
    /// </summary>
    public class ProgressBar
    {
        private readonly int BarLength = 32;

        private bool isRunning;
        private int position = 1;
        private bool positive = true;

        public ProgressBar(ProgressBarType type)
        {
            if (!Logger.isConsole)
                throw new Exception("This class (or function) can be used with console only. For UI please use another approach.");
            this.type = type;
        }

        public float Max { get; init; }
        public ConsoleColor Color { get; init; }
        public bool NoColor { get; init; }
        public ProgressBarType type { get; set; }

        public int TotalLength { get; private set; }


        public async void Start()
        {
            if (type != ProgressBarType.NO_END)
                throw new Exception("Only NO_END progress bar can use this method");
            if (isRunning)
                throw new Exception("This progress bar is already running");

            isRunning = true;
            while (isRunning)
            {
                UpdateNoEnd();
                await Task.Delay(100);
            }
        }

        public async void Start(string message)
        {
            if (type != ProgressBarType.NO_END)
                throw new Exception("Only NO_END progress bar can use this method");
            if (isRunning)
                throw new Exception("This progress bar is already running");

            isRunning = true;

            TotalLength = message.Length + BarLength + 5;
            while (isRunning)
            {
                UpdateNoEnd(message);
                await Task.Delay(100);
            }
        }

        public void Stop()
        {
            if (type != ProgressBarType.NO_END)
                throw new Exception("Only NO_END progress bar can use this method");
            if (!isRunning)
                throw new Exception("Can not stop a progressbar that did not start");
            isRunning = false;
        }

        public void Stop(string message)
        {
            Stop();

            if (message is not null)
            {
                Console.CursorLeft = 0;
                for (var i = 0; i < BarLength + message.Length + 1; i++)
                    Logger.Write(" ");
                Console.CursorLeft = 0;
                Logger.WriteLine(message);
            }
        }

        public void Update(float progress)
        {
            if (type == ProgressBarType.NO_END)
                throw new Exception("This function is for progress bars with end");

            UpdateNormal(progress);
        }

        private void UpdateNoEnd(string message)
        {
            Console.CursorLeft = 0;
            Logger.Write("[");
            for (var i = 1; i <= position; i++)
                Logger.Write(" ");
            Logger.Write("<==()==>");
            position += positive ? 1 : -1;
            for (var i = position; i <= BarLength - 1 - (positive ? 0 : 2); i++)
                Logger.Write(" ");
            Logger.Write("] " + message);


            if (position == BarLength - 1 || position == 1)
                positive = !positive;
        }

        private void UpdateNoEnd()
        {
            Console.CursorLeft = 0;
            Logger.Write("[");
            for (var i = 1; i <= position; i++)
                Logger.Write(" ");
            Logger.Write("<==()==>");
            position += positive ? 1 : -1;
            for (var i = position; i <= BarLength - 1 - (positive ? 0 : 2); i++)
                Logger.Write(" ");
            Logger.Write("]");


            if (position == BarLength - 1 || position == 1)
                positive = !positive;
        }

        private void UpdateNormal(float progress)
        {
            Console.CursorLeft = 0;
            Logger.Write("[");
            Console.CursorLeft = BarLength;
            Logger.Write("]");
            Console.CursorLeft = 1;
            var onechunk = 30.0f / Max;

            var position = 1;

            for (var i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = NoColor ? ConsoleColor.Black : Color;
                Console.CursorLeft = position++;
                Logger.Write("#");
            }

            for (var i = position; i < BarLength; i++)
            {
                Console.BackgroundColor = NoColor ? ConsoleColor.Black : ConsoleColor.DarkGray;
                Console.CursorLeft = position++;
                Logger.Write(" ");
            }

            Console.CursorLeft = BarLength + 4;
            Console.BackgroundColor = ConsoleColor.Black;
            if (progress.CanAproximateTo(Max))
                Logger.Write(progress + " %      ✓");
            else
                Logger.Write(MathF.Round(progress, 2) + " %       ");
        }
    }
}