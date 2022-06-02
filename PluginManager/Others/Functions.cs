using System.IO.Compression;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using Discord.WebSocket;
using PluginManager.Items;
using System.Threading;

namespace PluginManager.Others
{
    /// <summary>
    /// A special class with functions
    /// </summary>
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
        /// <returns>The value of the specified setting key code in the specified file (<see cref="string"/>)</returns>
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
                throw new FileNotFoundException("Failed to load file !");

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
                    File.AppendAllText(file, Code + separator + newValue + "\n");
                    ok = true;
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
        public static OperatingSystem GetOperatingSystem()
        {
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows)) return OperatingSystem.WINDOWS;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) return OperatingSystem.LINUX;
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) return OperatingSystem.MAC_OS;
            return OperatingSystem.UNKNOWN;
        }

        public static List<string> GetArguments(SocketMessage message)
        {
            Command command = new Command(message);
            return command.Arguments;
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

        /// <summary>
        /// Copy one Stream to another <see langword="async"/>
        /// </summary>
        /// <param name="stream">The base stream</param>
        /// <param name="destination">The destination stream</param>
        /// <param name="bufferSize">The buffer to read</param>
        /// <param name="progress">The progress</param>
        /// <param name="cancellationToken">The cancellation token</param>
        /// <exception cref="ArgumentNullException">Triggered if any <see cref="Stream"/> is empty</exception>
        /// <exception cref="ArgumentOutOfRangeException">Triggered if <paramref name="bufferSize"/> is less then or equal to 0</exception>
        /// <exception cref="InvalidOperationException">Triggered if <paramref name="stream"/> is not readable</exception>
        /// <exception cref="ArgumentException">Triggered in <paramref name="destination"/> is not writable</exception>
        public static async Task CopyToOtherStreamAsync(this Stream stream, Stream destination, int bufferSize, IProgress<long>? progress = null, CancellationToken cancellationToken = default)
        {
            if (stream == null)
                throw new ArgumentNullException(nameof(stream));
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (bufferSize <= 0)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (!stream.CanRead)
                throw new InvalidOperationException("The stream is not readable.");
            if (!destination.CanWrite)
                throw new ArgumentException("Destination stream is not writable", nameof(destination));

            byte[] buffer = new byte[bufferSize];
            long totalBytesRead = 0;
            int bytesRead;
            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) != 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
                totalBytesRead += bytesRead;
                progress?.Report(totalBytesRead);
            }

        }

        /// <summary>
        /// Extract zip to location
        /// </summary>
        /// <param name="zip">The zip location</param>
        /// <param name="folder">The target location</param>
        /// <returns></returns>
        public static async Task ExtractArchive(string zip, string folder, IProgress<float> progress)
        {
            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);




            using (ZipArchive archive = ZipFile.OpenRead(zip))
            {
                int totalZIPFiles = archive.Entries.Count();
                int currentZIPFile = 0;
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/"))
                        Directory.CreateDirectory(Path.Combine(folder, entry.FullName));

                    else
                        try { entry.ExtractToFile(Path.Combine(folder, entry.FullName), true); }
                        catch { }

                    currentZIPFile++;
                    await Task.Delay(10);
                    progress.Report((float)currentZIPFile / totalZIPFiles * 100);
                }
            }
        }


        public static (double, string) ConvertBytes(long bytes)
        {
            if (bytes < 1024) return (bytes, "B");
            if (bytes < 1024 * 1024) return (bytes / 1024.0, "KB");
            if (bytes < 1024 * 1024 * 1024) return (bytes / 1024.0 / 1024.0, "MB");
            return (bytes / 1024.0 / 1024.0 / 1024.0, "GB");

        }
    }
}
