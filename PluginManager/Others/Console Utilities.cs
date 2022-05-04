using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PluginManager.Others
{
    public class Console_Utilities
    {
        /// <summary>
        /// Progress bar object
        /// </summary>
        public class ProgressBar
        {
            public int Progress { get; set; }
            public int Max { get; set; }
            public string Message { get; set; }

            public ProgressBar(int max, string message)
            {
                Max = max;
                Message = message;
            }

            public async void Update(int progress, bool r = false)
            {

                //progress bar
                Console.CursorLeft = 0;
                Console.Write("[");
                Console.CursorLeft = 32;
                Console.Write("]");
                Console.CursorLeft = 1;
                float onechunk = 30.0f / Max;

                int position = 1;

                for (int i = 0; i < onechunk * progress; i++)
                {
                    Console.BackgroundColor = ConsoleColor.Green;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                for (int i = position; i <= 31; i++)
                {
                    Console.BackgroundColor = ConsoleColor.Gray;
                    Console.CursorLeft = position++;
                    Console.Write(" ");
                }

                Console.CursorLeft = 35;
                Console.BackgroundColor = ConsoleColor.Black;
                Console.Write(progress.ToString() + " of " + Max.ToString() + "    ");

                if (r == false)
                    Update(progress, true);

            }

            public void Finish()
            {
                Console.Write("\r{0} {1}%", Message, 100);
                Console.WriteLine();
            }
        }


        /// <summary>
        /// A way to create a table based on input data
        /// </summary>
        /// <param name="data">The List of arrays of strings that represent the rows.</param>
        public static void FormatAndAlignTable(List<string[]> data)
        {
            char tableLine = '-';
            char tableCross = '+';
            char tableWall = '|';

            int[] len = new int[data[0].Length];
            foreach (var line in data)
            {
                for (int i = 0; i < line.Length; i++)
                    if (line[i].Length > len[i])
                        len[i] = line[i].Length;
            }


            foreach (string[] row in data)
            {
                //Console.Write("\t");
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

                    if (row[l][0] == tableLine) Console.Write(tableCross);
                    else Console.Write(tableWall);
                }
                Console.WriteLine(); //end line

            }
        }

        /// <summary>
        /// Write the text using color options( &g-green; &b-blue; &r-red; &c-clear; )
        /// 
        /// </summary>
        /// <param name="text">The text</param>
        public static void WriteColorText(string text, bool appendNewLine = true)
        {
            string[] words = text.Split(' ');
            Dictionary<string, ConsoleColor> colors = new Dictionary<string, ConsoleColor>()
            {
                {"&g", ConsoleColor.Green },
                {"&b", ConsoleColor.Blue  },
                {"&r", ConsoleColor.Red  },
                {"&m", ConsoleColor.Magenta },
                {"&c", Console.ForegroundColor }
            };
            foreach (string word in words)
            {
                if (word.Length >= 2)
                {
                    string prefix = word.Substring(0, 2);
                    if (colors.ContainsKey(prefix))
                        Console.ForegroundColor = colors[prefix];
                }

                string m = word.Replace("&g", "").Replace("&b", "").Replace("&r", "").Replace("&c", "").Replace("&m", "");
                Console.Write(m + " ");
            }
            if (appendNewLine)
                Console.Write('\n');
        }

    }
}
