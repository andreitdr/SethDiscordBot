using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others
{
    public class Console_Utilities
    {
        const char _block = '■';
        const string _back = "\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b\b";
        const string _twirl = "-\\|/";
        public static void WriteProgressBar(int percent, bool update = false)
        {
            if (update)
                Console.Write(_back);
            Console.Write("[");
            var p = (int)((percent / 10f) + .5f);
            for (var i = 0; i < 10; ++i)
            {
                if (i >= p)
                    Console.Write(' ');
                else
                    Console.Write(_block);
            }
            Console.Write("] {0,3:##0}%", percent);

            if (percent == 100)
                Console.WriteLine();
        }
        public static void WriteProgress(int progress, bool update = false)
        {
            if (update)
                Console.Write("\b");
            Console.Write(_twirl[progress % _twirl.Length]);
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


            //Obsolite
            #region Old Code -> Spacing by the lomgest item in any cell
            /*
            int maxLen = 0;
            foreach (string[] row in data)
                foreach (string s in row)
                    if (s.Length > maxLen)
                        maxLen = s.Length;

            int div = (maxLen + 4) / 2;

            foreach (string[] row in data)
            {
                //Console.Write("\t");
                if (row[0] == "-") Console.Write("+");
                else Console.Write("|");

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

                    if (s == "-") Console.Write("+");
                    else Console.Write("|");
                }
                Console.WriteLine(); //end line
            }*/
            #endregion
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
