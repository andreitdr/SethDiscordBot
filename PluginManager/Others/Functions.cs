﻿using System.IO.Compression;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;


namespace PluginManager.Others
{
    public static class Functions
    {
        /// <summary>
        /// The location for the Resources folder
        /// </summary>
        public static readonly string dataFolder = @"./Data/Resources/";

        /// <summary>
        /// The location for all logs
        /// </summary>
        public static readonly string logFolder = @"./Output/Logs/";

        /// <summary>
        /// The location for all errors
        /// </summary>
        public static readonly string errFolder = @"./Output/Errors/";

        /// <summary>
        /// The location for all languages
        /// </summary>
        public static readonly string langFolder = @"./Data/Languages/";

        /// <summary>
        /// Archives folder
        /// </summary>
        public static readonly string pakFolder = @"./Data/Resources/PAKS/";

        /// <summary>
        /// The mark that the line is a comment
        /// </summary>
        private static readonly char commentMark = '#';

        /// <summary>
        /// Read data from file
        /// </summary>
        /// <param name="fileName">File name</param>
        /// <param name="Code">Setting name</param>
        /// <param name="separator">Separator between setting key code and its value</param>
        /// <returns>The value of the specified setting key code in the specified file (STRING)</returns>
        public static string? readCodeFromFile(string fileName, string Code, char separator)
          => File.ReadAllLines(fileName)
            .Where(p => p.StartsWith(Code) && !p.StartsWith(commentMark.ToString()))
            .First().Split(separator)[1] ?? null;

        /// <summary>
        /// Read data from a file that is inside an archive (ZIP format)
        /// </summary>
        /// <param name="FileName">The file name that is inside the archive or its full path</param>
        /// <param name="archFile">The archive location from the PAKs folder</param> 
        /// <returns>A string that represents the content of the file or null if the file does not exists or it has no content</returns>
        public static async Task<string?> ReadFromPakAsync(string FileName, string archFile)
        {
            archFile = pakFolder + archFile;
            Directory.CreateDirectory(pakFolder);
            if (!File.Exists(archFile))
            {
                throw new Exception("Failed to load file !");
            }

            string? textValue = null;
            var fs = new FileStream(archFile, FileMode.Open);
            var zip = new ZipArchive(fs, ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == FileName || entry.FullName == FileName)
                {
                    Stream s = entry.Open();
                    StreamReader reader = new StreamReader(s);
                    textValue = await reader.ReadToEndAsync();
                    reader.Close();
                    s.Close();
                    fs.Close();
                    break;
                }
            }
            return textValue;
        }

        /// <summary>
        /// Write logs to file
        /// </summary>
        /// <param name="LogMessage">The message to be wrote</param>
        public static void WriteLogFile(string LogMessage)
        {
            string logsPath = logFolder + "Log.txt";
            if (!Directory.Exists(logFolder))
                Directory.CreateDirectory(logFolder);
            File.AppendAllText(logsPath, LogMessage + " \n");
        }

        /// <summary>
        /// Write error to file
        /// </summary>
        /// <param name="ErrMessage">The message to be wrote</param>
        public static void WriteErrFile(string ErrMessage)
        {
            string errPath = errFolder + "Error.txt";
            if (!Directory.Exists(errFolder))
                Directory.CreateDirectory(errFolder);
            File.AppendAllText(errPath, ErrMessage + " \n");
        }

        /// <summary>
        /// Write to settings file
        /// </summary>
        /// <param name="file">The settings file path</param>
        /// <param name="Code">The Key value of the setting</param>
        /// <param name="newValue">The new value of the settings</param>
        /// <param name="separator">The separator between the key and the value</param>
        public static void WriteToSettings(string file, string Code, string newValue, char separator)
        {

            string[] lines = File.ReadAllLines(file);
            File.Delete(file);
            bool ok = false;
            foreach (var line in lines)
                if (line.StartsWith(Code))
                {
                    File.AppendAllText(file, Code + separator + newValue + "\n"); ok = true;
                }
                else File.AppendAllText(file, line + "\n");

            if (!ok)
                File.AppendAllText(file, Code + separator + newValue + "\n");
        }

        /// <summary>
        /// Merge one array of strings into one string
        /// </summary>
        /// <param name="s">The array of strings</param>
        /// <param name="indexToStart">The index from where the merge should start (included)</param>
        /// <returns>A string built based on the array</returns>
        public static string MergeStrings(this string[] s, int indexToStart)
        {
            string r = "";
            int len = s.Length;
            if (len <= indexToStart) return "";
            for (int i = indexToStart; i < len - 1; ++i)
            {
                r += s[i] + " ";
            }

            r += s[len - 1];

            return r;
        }

        /// <summary>
        /// Get the Operating system you are runnin on
        /// </summary>
        /// <returns>An Operating system</returns>
        public static OperatingSystem GetOperatinSystem()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) return OperatingSystem.WINDOWS;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) return OperatingSystem.LINUX;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) return OperatingSystem.MAC_OS;
            return OperatingSystem.UNKNOWN;
        }

        /// <summary>
        /// A way to create a table based on input data
        /// EpicWings (Pasca Robert) este cel mai bun
        /// Special thanks to Kami-sama <3
        /// </summary>
        /// <param name="data">The List of arrays of strings that represent the rows.</param>
        public static void FormatAndAlignTable(List<string[]> data)
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
            }
        }

        /// <summary>
        /// Write the text using color options( &g-green; &b-blue; &r-red; &c-clear; )
        /// </summary>
        /// <param name="text">The text</param>
        public static void WriteColorText(string text)
        {
            string[] words = text.Split(' ');
            Dictionary<string, ConsoleColor> colors = new Dictionary<string, ConsoleColor>()
            {
                {"&g", ConsoleColor.Green },
                {"&b", ConsoleColor.Blue  },
                {"&r", ConsoleColor.Red  },
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

                string m = word.Replace("&g", "").Replace("&b", "").Replace("&r", "").Replace("&c", "");
                Console.Write(m + " ");
            }
            Console.Write('\n');
        }

        /// <summary>
        /// Write setting 
        /// </summary>
        /// <param name="SettingName">The full path to the setting</param>
        /// <param name="NewValue">The new Value</param>
        public static void WriteToSettingsFast(string SettingName, string NewValue)
        {

            string path = dataFolder; // Resources/

            string[] args = SettingName.Split('.');

            int len = args.Length;
            if (len < 2) return;
            for (int i = 0; i < len - 2; i++)
                path += args[i] + "/";
            path += args[len - 2] + ".txt";


            WriteToSettings(path, args[len - 1].Replace('_', ' '), NewValue, '=');

        }

        public static string FromHexToString(string hexString)
        {
            var bytes = new byte[hexString.Length / 2];
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }

            return System.Text.Encoding.Unicode.GetString(bytes);
        }

        public static string ToHexString(string str)
        {
            var sb = new System.Text.StringBuilder();

            var bytes = System.Text.Encoding.Unicode.GetBytes(str);
            foreach (var t in bytes)
            {
                sb.Append(t.ToString("X2"));
            }

            return sb.ToString();
        }
    }
}