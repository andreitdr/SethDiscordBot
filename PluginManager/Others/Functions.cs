using System.IO.Compression;
using System.IO;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using System.Security.Cryptography;
using Discord.WebSocket;
using PluginManager.Items;
using System.Threading;
using System.Text.Json;
using System.Text;

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
        /// Archives folder
        /// </summary>
        public static readonly string pakFolder = @"./Data/Resources/PAK/";

        /// <summary>
        /// Beta testing folder
        /// </summary>
        public static readonly string betaFolder = @"./Data/BetaTest/";


        /// <summary>
        /// Read data from a file that is inside an archive (ZIP format)
        /// </summary>
        /// <param name="FileName">The file name that is inside the archive or its full path</param>
        /// <param name="archFile">The archive location from the PAKs folder</param> 
        /// <returns>A string that represents the content of the file or null if the file does not exists or it has no content</returns>
        public static async Task<Stream?> ReadFromPakAsync(string FileName, string archFile)
        {
            archFile = pakFolder + archFile;
            Directory.CreateDirectory(pakFolder);
            if (!File.Exists(archFile)) throw new FileNotFoundException("Failed to load file !");

            Stream? textValue = null;
            var     fs        = new FileStream(archFile, FileMode.Open);
            var     zip       = new ZipArchive(fs, ZipArchiveMode.Read);
            foreach (var entry in zip.Entries)
            {
                if (entry.Name == FileName || entry.FullName == FileName)
                {
                    Stream       s      = entry.Open();
                    StreamReader reader = new StreamReader(s);
                    textValue          = reader.BaseStream;
                    textValue.Position = 0;
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
            string logsPath = logFolder + $"{DateTime.Today.ToShortDateString().Replace("/", "-").Replace("\\", "-")} Log.txt";
            if (!Directory.Exists(logFolder)) Directory.CreateDirectory(logFolder);
            File.AppendAllText(logsPath, LogMessage + " \n");
        }

        /// <summary>
        /// Write error to file
        /// </summary>
        /// <param name="ErrMessage">The message to be wrote</param>
        public static void WriteErrFile(string ErrMessage)
        {
            string errPath = errFolder + $"{DateTime.Today.ToShortDateString().Replace("/", "-").Replace("\\", "-")} Error.txt";
            if (!Directory.Exists(errFolder)) Directory.CreateDirectory(errFolder);
            File.AppendAllText(errPath, ErrMessage + " \n");
        }

        /// <summary>
        /// Merge one array of strings into one string
        /// </summary>
        /// <param name="s">The array of strings</param>
        /// <param name="indexToStart">The index from where the merge should start (included)</param>
        /// <returns>A string built based on the array</returns>
        public static string MergeStrings(this string[] s, int indexToStart)
        {
            string r   = "";
            int    len = s.Length;
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
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            if (destination == null) throw new ArgumentNullException(nameof(destination));
            if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
            if (!stream.CanRead) throw new InvalidOperationException("The stream is not readable.");
            if (!destination.CanWrite) throw new ArgumentException("Destination stream is not writable", nameof(destination));

            byte[] buffer         = new byte[bufferSize];
            long   totalBytesRead = 0;
            int    bytesRead;
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
        /// <param name="progress">The progress that is updated as a file is processed</param>
        /// <param name="type">The type of progress</param>
        /// <returns></returns>
        public static async Task ExtractArchive(string zip, string folder, IProgress<float> progress, UnzipProgressType type)
        {
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);


            using (ZipArchive archive = ZipFile.OpenRead(zip))
            {
                if (type == UnzipProgressType.PercentageFromNumberOfFiles)
                {
                    int totalZIPFiles  = archive.Entries.Count();
                    int currentZIPFile = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("/"))
                            Directory.CreateDirectory(Path.Combine(folder, entry.FullName));

                        else
                            try
                            {
                                entry.ExtractToFile(Path.Combine(folder, entry.FullName), true);
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Failed to extract {entry.Name}. Exception: {ex.Message}");
                            }

                        currentZIPFile++;
                        await Task.Delay(10);
                        progress.Report((float)currentZIPFile / totalZIPFiles * 100);
                    }
                }
                else if (type == UnzipProgressType.PercentageFromTotalSize)
                {
                    ulong zipSize = 0;

                    foreach (ZipArchiveEntry entry in archive.Entries)
                        zipSize += (ulong)entry.CompressedLength;

                    ulong currentSize = 0;
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.FullName.EndsWith("/"))
                        {
                            Directory.CreateDirectory(Path.Combine(folder, entry.FullName));
                            continue;
                        }

                        try
                        {
                            entry.ExtractToFile(Path.Combine(folder, entry.FullName), true);
                            currentSize += (ulong)entry.CompressedLength;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to extract {entry.Name}. Exception: {ex.Message}");
                        }

                        await Task.Delay(10);
                        progress.Report((float)currentSize / zipSize * 100);
                    }
                }
            }
        }


        /// <summary>
        /// Convert Bytes to highest measurement unit possible
        /// </summary>
        /// <param name="bytes">The amount of bytes</param>
        /// <returns></returns>
        public static (double, string) ConvertBytes(long bytes)
        {
            List<string> units = new List<string>()
            {
                "B",
                "KB",
                "MB",
                "GB",
                "TB"
            };
            int i = 0;
            while (bytes >= 1024)
            {
                i++;
                bytes /= 1024;
            }

            return (bytes, units[i]);
        }

        /// <summary>
        /// Save to JSON file
        /// </summary>
        /// <typeparam name="T">The class type</typeparam>
        /// <param name="file">The file path</param>
        /// <param name="Data">The values</param>
        /// <returns></returns>
        public static async Task SaveToJsonFile<T>(string file, T Data)
        {
            var s = File.OpenWrite(file);
            await JsonSerializer.SerializeAsync(s, Data, typeof(T), new JsonSerializerOptions { WriteIndented = true });
            s.Close();
        }

        /// <summary>
        /// Convert json text or file to some kind of data
        /// </summary>
        /// <typeparam name="T">The data type</typeparam>
        /// <param name="input">The file or json text</param>
        /// <returns></returns>
        public static async Task<T> ConvertFromJson<T>(string input)
        {
            Stream text;
            if (File.Exists(input))
                text = File.Open(input, FileMode.OpenOrCreate);

            else
                text = new MemoryStream(Encoding.ASCII.GetBytes(input));
            text.Position = 0;
            var obj = await JsonSerializer.DeserializeAsync<T>(text);
            text.Close();
            return (obj ?? default)!;
        }

        /// <summary>
        /// Check if all words from <paramref name="str"/> are in <paramref name="baseString"/><br/>
        /// This function returns true if<br/>
        /// 1. The <paramref name="str"/> is part of <paramref name="baseString"/><br/>
        /// 2. The words (split by a space) of <paramref name="str"/> are located (separately) in <paramref name="baseString"/> <br/>
        /// <example>
        /// The following example will return <see langword="TRUE"/><br/>
        /// <c>STRContains("Hello World !", "I type word Hello and then i typed word World !")</c><br/>
        /// The following example will return <see langword="TRUE"/><br/>
        /// <c>STRContains("Hello World !", "I typed Hello World !" </c><br/>
        /// The following example will return <see langword="TRUE"/><br/>
        ///  <c>STRContains("Hello World", "I type World then Hello")</c><br/>
        /// The following example will return <see langword="FALSE"/><br/>
        /// <c>STRContains("Hello World !", "I typed Hello World")</c><br/>
        /// </example>
        /// </summary>
        /// <param name="str">The string you are checking</param>
        /// <param name="baseString">The main string that should contain <paramref name="str"/></param>
        /// <returns></returns>
        public static bool STRContains(this string str, string baseString)
        {
            if (baseString.Contains(str)) return true;
            string[] array = str.Split(' ');
            foreach (var s in array)
                if (!baseString.Contains(s))
                    return false;
            return true;
        }

        public static bool TryReadValueFromJson(string input, string codeName, out JsonElement element)
        {
            Stream text;
            if (File.Exists(input))
                text = File.OpenRead(input);

            else
                text = new MemoryStream(Encoding.ASCII.GetBytes(input));

            var jsonObject = JsonDocument.Parse(text);

            var data = jsonObject.RootElement.TryGetProperty(codeName, out element);
            return data;
        }

        public static string CreateMD5(string input)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes  = md5.ComputeHash(inputBytes);
                return Convert.ToHexString(hashBytes);
            }
        }
    }
}
