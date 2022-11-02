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
                    Settings.Variables.outputStream.Write(tableCross);
                else
                    Settings.Variables.outputStream.Write(tableWall);
                for (var l = 0; l < row.Length; l++)
                {
                    if (row[l][0] == tableLine)
                    {
                        for (var i = 0; i < len[l] + 4; ++i)
                            Settings.Variables.outputStream.Write(tableLine);
                    }
                    else if (row[l].Length == len[l])
                    {
                        Settings.Variables.outputStream.Write("  ");
                        Settings.Variables.outputStream.Write(row[l]);
                        Settings.Variables.outputStream.Write("  ");
                    }
                    else
                    {
                        var lenHalf = row[l].Length / 2;
                        for (var i = 0; i < (len[l] + 4) / 2 - lenHalf; ++i)
                            Settings.Variables.outputStream.Write(" ");
                        Settings.Variables.outputStream.Write(row[l]);
                        for (var i = (len[l] + 4) / 2 + lenHalf + 1; i < len[l] + 4; ++i)
                            Settings.Variables.outputStream.Write(" ");
                        if (row[l].Length % 2 == 0)
                            Settings.Variables.outputStream.Write(" ");
                    }

                    Settings.Variables.outputStream.Write(row[l][0] == tableLine ? tableCross : tableWall);
                }

                Settings.Variables.outputStream.WriteLine(); //end line
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
                Settings.Variables.outputStream.Write("\t");
                if (row[0] == "-")
                    Settings.Variables.outputStream.Write("+");
                else
                    Settings.Variables.outputStream.Write("|");

                foreach (var s in row)
                {
                    if (s == "-")
                    {
                        for (var i = 0; i < maxLen + 4; ++i)
                            Settings.Variables.outputStream.Write("-");
                    }
                    else if (s.Length == maxLen)
                    {
                        Settings.Variables.outputStream.Write("  ");
                        Settings.Variables.outputStream.Write(s);
                        Settings.Variables.outputStream.Write("  ");
                    }
                    else
                    {
                        var lenHalf = s.Length / 2;
                        for (var i = 0; i < div - lenHalf; ++i)
                            Settings.Variables.outputStream.Write(" ");
                        Settings.Variables.outputStream.Write(s);
                        for (var i = div + lenHalf + 1; i < maxLen + 4; ++i)
                            Settings.Variables.outputStream.Write(" ");
                        if (s.Length % 2 == 0)
                            Settings.Variables.outputStream.Write(" ");
                    }

                    if (s == "-")
                        Settings.Variables.outputStream.Write("+");
                    else
                        Settings.Variables.outputStream.Write("|");
                }

                Settings.Variables.outputStream.WriteLine(); //end line
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
                    Settings.Variables.outputStream.Write(data[i][j]);
                    for (var k = 0; k < widths[j] - data[i][j].Length + 1 + space_between_columns; k++)
                        Settings.Variables.outputStream.Write(" ");
                }

                Settings.Variables.outputStream.WriteLine();
            }

            return;
        }

        throw new Exception("Unknown type of table");
    }

    public static void WriteColorText(string text, bool appendNewLineAtEnd = true)
    {
        if (Console.Out != Settings.Variables.outputStream)
        {
            Settings.Variables.outputStream.Write(text);
            if (appendNewLineAtEnd)
                Settings.Variables.outputStream.WriteLine();
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
                Settings.Variables.outputStream.Write(input[i]);
            }

        Console.ForegroundColor = initialForeGround;
        if (appendNewLineAtEnd)
            Settings.Variables.outputStream.WriteLine();
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
            if (Settings.Variables.outputStream != Console.Out)
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
                    Settings.Variables.outputStream.Write(" ");
                Console.CursorLeft = 0;
                Settings.Variables.outputStream.WriteLine(message);
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
            Settings.Variables.outputStream.Write("[");
            for (var i = 1; i <= position; i++)
                Settings.Variables.outputStream.Write(" ");
            Settings.Variables.outputStream.Write("<==()==>");
            position += positive ? 1 : -1;
            for (var i = position; i <= BarLength - 1 - (positive ? 0 : 2); i++)
                Settings.Variables.outputStream.Write(" ");
            Settings.Variables.outputStream.Write("] " + message);


            if (position == BarLength - 1 || position == 1)
                positive = !positive;
        }

        private void UpdateNoEnd()
        {
            Console.CursorLeft = 0;
            Settings.Variables.outputStream.Write("[");
            for (var i = 1; i <= position; i++)
                Settings.Variables.outputStream.Write(" ");
            Settings.Variables.outputStream.Write("<==()==>");
            position += positive ? 1 : -1;
            for (var i = position; i <= BarLength - 1 - (positive ? 0 : 2); i++)
                Settings.Variables.outputStream.Write(" ");
            Settings.Variables.outputStream.Write("]");


            if (position == BarLength - 1 || position == 1)
                positive = !positive;
        }

        private void UpdateNormal(float progress)
        {
            Console.CursorLeft = 0;
            Settings.Variables.outputStream.Write("[");
            Console.CursorLeft = BarLength;
            Settings.Variables.outputStream.Write("]");
            Console.CursorLeft = 1;
            var onechunk = 30.0f / Max;

            var position = 1;

            for (var i = 0; i < onechunk * progress; i++)
            {
                Console.BackgroundColor = NoColor ? ConsoleColor.Black : Color;
                Console.CursorLeft = position++;
                Settings.Variables.outputStream.Write("#");
            }

            for (var i = position; i < BarLength; i++)
            {
                Console.BackgroundColor = NoColor ? ConsoleColor.Black : ConsoleColor.DarkGray;
                Console.CursorLeft = position++;
                Settings.Variables.outputStream.Write(" ");
            }

            Console.CursorLeft = BarLength + 4;
            Console.BackgroundColor = ConsoleColor.Black;
            if (progress.CanAproximateTo(Max))
                Settings.Variables.outputStream.Write(progress + " %      ✓");
            else
                Settings.Variables.outputStream.Write(MathF.Round(progress, 2) + " %       ");
        }
    }
}