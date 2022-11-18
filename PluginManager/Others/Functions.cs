using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Discord.WebSocket;

using PluginManager.Items;

namespace PluginManager.Others;

/// <summary>
///     A special class with functions
/// </summary>
public static class Functions
{
    /// <summary>
    ///     The location for the Resources folder
    /// </summary>
    public static readonly string dataFolder = @"./Data/Resources/";

    /// <summary>
    ///     The location for all logs
    /// </summary>
    public static readonly string logFolder = @"./Data/Output/Logs/";

    /// <summary>
    ///     The location for all errors
    /// </summary>
    public static readonly string errFolder = @"./Data/Output/Errors/";

    /// <summary>
    ///     Archives folder
    /// </summary>
    public static readonly string pakFolder = @"./Data/PAKS/";


    /// <summary>
    ///     Read data from a file that is inside an archive (ZIP format)
    /// </summary>
    /// <param name="FileName">The file name that is inside the archive or its full path</param>
    /// <param name="archFile">The archive location from the PAKs folder</param>
    /// <returns>A string that represents the content of the file or null if the file does not exists or it has no content</returns>
    public static async Task<string> ReadFromPakAsync(string FileName, string archFile)
    {
        archFile = pakFolder + archFile;
        if (!File.Exists(archFile))
            throw new Exception("Failed to load file !");

        try
        {
            string textValue = null;
            using (var fs = new FileStream(archFile, FileMode.Open))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                    if (entry.Name == FileName || entry.FullName == FileName)
                        using (var s = entry.Open())
                        using (var reader = new StreamReader(s))
                        {
                            textValue = await reader.ReadToEndAsync();
                            reader.Close();
                            s.Close();
                            fs.Close();
                        }
            }

            return textValue;
        }
        catch
        {
            await Task.Delay(100);
            return await ReadFromPakAsync(FileName, archFile);
        }
    }

    /// <summary>
    /// Read content of a packed file as a <see cref="Stream"/>
    /// </summary>
    /// <param name="fileName">The file name inside the archive</param>
    /// <param name="archFile">The archive name</param>
    public static async Task<Stream> ReadStreamFromPAKAsync(string fileName, string archFile)
    {

        archFile = pakFolder + archFile;
        if (!File.Exists(archFile))
            throw new Exception("Failed to load file !");

        try
        {
            Stream stream = Stream.Null;
            using (var fs = new FileStream(archFile, FileMode.Open))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                    if (entry.Name == fileName || entry.FullName == fileName)
                        stream = entry.Open();
            }

            return stream;
        }
        catch (Exception ex)
        {
            ex.WriteErrFile();
            await Task.Delay(100);
            return await ReadStreamFromPAKAsync(fileName, archFile);
        }
    }


    /// <summary>
    ///     Write logs to file
    /// </summary>
    /// <param name="LogMessage">The message to be wrote</param>
    public static void WriteLogFile(string LogMessage)
    {
        var logsPath = logFolder + $"{DateTime.Today.ToShortDateString().Replace("/", "-").Replace("\\", "-")} Log.txt";
        Directory.CreateDirectory(logFolder);
        File.AppendAllText(logsPath, LogMessage + " \n");
    }

    /// <summary>
    ///     Write error to file
    /// </summary>
    /// <param name="ErrMessage">The message to be wrote</param>
    public static void WriteErrFile(string ErrMessage)
    {
        var errPath = errFolder +
                      $"{DateTime.Today.ToShortDateString().Replace("/", "-").Replace("\\", "-")} Error.txt";
        Directory.CreateDirectory(errFolder);
        File.AppendAllText(errPath, ErrMessage + " \n");
    }

    public static void WriteErrFile(this Exception ex)
    {
        WriteErrFile(ex.ToString());
    }

    /// <summary>
    ///     Get the Operating system you are runnin on
    /// </summary>
    /// <returns>An Operating system</returns>
    public static OperatingSystem GetOperatingSystem()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return OperatingSystem.WINDOWS;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) return OperatingSystem.LINUX;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) return OperatingSystem.MAC_OS;
        return OperatingSystem.UNKNOWN;
    }

    public static List<string> GetArguments(SocketMessage message)
    {
        var command = new Command(message);
        return command.Arguments;
    }

    /// <summary>
    ///     Copy one Stream to another <see langword="async" />
    /// </summary>
    /// <param name="stream">The base stream</param>
    /// <param name="destination">The destination stream</param>
    /// <param name="bufferSize">The buffer to read</param>
    /// <param name="progress">The progress</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <exception cref="ArgumentNullException">Triggered if any <see cref="Stream" /> is empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Triggered if <paramref name="bufferSize" /> is less then or equal to 0</exception>
    /// <exception cref="InvalidOperationException">Triggered if <paramref name="stream" /> is not readable</exception>
    /// <exception cref="ArgumentException">Triggered in <paramref name="destination" /> is not writable</exception>
    public static async Task CopyToOtherStreamAsync(this Stream stream, Stream destination, int bufferSize,
                                                    IProgress<long>? progress = null,
                                                    CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (!stream.CanRead) throw new InvalidOperationException("The stream is not readable.");
        if (!destination.CanWrite)
            throw new ArgumentException("Destination stream is not writable", nameof(destination));

        var buffer = new byte[bufferSize];
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
    ///     Extract zip to location
    /// </summary>
    /// <param name="zip">The zip location</param>
    /// <param name="folder">The target location</param>
    /// <param name="progress">The progress that is updated as a file is processed</param>
    /// <param name="type">The type of progress</param>
    /// <returns></returns>
    public static async Task ExtractArchive(string zip, string folder, IProgress<float> progress,
                                            UnzipProgressType type)
    {
        Directory.CreateDirectory(folder);
        using (var archive = ZipFile.OpenRead(zip))
        {
            if (type == UnzipProgressType.PercentageFromNumberOfFiles)
            {
                var totalZIPFiles = archive.Entries.Count();
                var currentZIPFile = 0;
                foreach (var entry in archive.Entries)
                {
                    if (entry.FullName.EndsWith("/")) // it is a folder
                        Directory.CreateDirectory(Path.Combine(folder, entry.FullName));

                    else
                        try
                        {
                            entry.ExtractToFile(Path.Combine(folder, entry.FullName), true);
                        }
                        catch (Exception ex)
                        {
                            Logger.WriteLine($"Failed to extract {entry.Name}. Exception: {ex.Message}");
                        }

                    currentZIPFile++;
                    await Task.Delay(10);
                    if (progress != null)
                        progress.Report((float)currentZIPFile / totalZIPFiles * 100);
                }
            }
            else if (type == UnzipProgressType.PercentageFromTotalSize)
            {
                ulong zipSize = 0;

                foreach (var entry in archive.Entries)
                    zipSize += (ulong)entry.CompressedLength;

                ulong currentSize = 0;
                foreach (var entry in archive.Entries)
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
                        Logger.WriteLine($"Failed to extract {entry.Name}. Exception: {ex.Message}");
                    }

                    await Task.Delay(10);
                    if (progress != null)
                        progress.Report((float)currentSize / zipSize * 100);
                }
            }
        }
    }

    /// <summary>
    ///     Save to JSON file
    /// </summary>
    /// <typeparam name="T">The class type</typeparam>
    /// <param name="file">The file path</param>
    /// <param name="Data">The values</param>
    /// <returns></returns>
    public static async Task SaveToJsonFile<T>(string file, T Data)
    {
        var str = new MemoryStream();
        await JsonSerializer.SerializeAsync(str, Data, typeof(T), new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllBytesAsync(file, str.ToArray());
    }

    /// <summary>
    ///     Convert json text or file to some kind of data
    /// </summary>
    /// <typeparam name="T">The data type</typeparam>
    /// <param name="input">The file or json text</param>
    /// <returns></returns>
    public static async Task<T> ConvertFromJson<T>(string input)
    {
        Stream text;
        if (File.Exists(input))
            text = new MemoryStream(await File.ReadAllBytesAsync(input));
        else
            text = new MemoryStream(Encoding.ASCII.GetBytes(input));
        text.Position = 0;
        var obj = await JsonSerializer.DeserializeAsync<T>(text);
        text.Close();
        return (obj ?? default)!;
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
        using (var md5 = MD5.Create())
        {
            var inputBytes = Encoding.ASCII.GetBytes(input);
            var hashBytes = md5.ComputeHash(inputBytes);
            return Convert.ToHexString(hashBytes);
        }
    }
}