using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager.Others;

public static class ArchiveManager
{
    private static string? archiveFolder;
    public static bool isInitialized { get; private set; }

    public static void Initialize()
    {
        if (isInitialized) throw new Exception("ArchiveManager is already initialized");

        if (!Config.AppSettings.ContainsKey("ArchiveFolder"))
            Config.AppSettings["ArchiveFolder"] = "./Data/PAKS/";

        archiveFolder = Config.AppSettings["ArchiveFolder"];

        isInitialized = true;
    }

    /// <summary>
    /// Read a file from a zip archive. The output is a byte array
    /// </summary>
    /// <param name="fileName">The file name in the archive</param>
    /// <param name="archName">The archive location on the disk</param>
    /// <returns>An array of bytes that represents the Stream value from the file that was read inside the archive</returns>
    public async static Task<byte[]?> ReadStreamFromPakAsync(string fileName, string archName)
    {
        if (!isInitialized) throw new Exception("ArchiveManager is not initialized");

        archName = archiveFolder + archName;

        if (!File.Exists(archName))
            throw new Exception("Failed to load file !");

        byte[]? data = null;

        using (var zip = ZipFile.OpenRead(archName))
        {
            var entry = zip.Entries.FirstOrDefault(entry => entry.FullName == fileName || entry.Name == fileName);
            if (entry is null) throw new Exception("File not found in archive");

            var MemoryStream = new MemoryStream();

            var stream = entry.Open();
            await stream.CopyToAsync(MemoryStream);
            data = MemoryStream.ToArray();

            stream.Close();
            MemoryStream.Close();
        }

        return data;
    }

    /// <summary>
    ///     Read data from a file that is inside an archive (ZIP format)
    /// </summary>
    /// <param name="FileName">The file name that is inside the archive or its full path</param>
    /// <param name="archFile">The archive location from the PAKs folder</param>
    /// <returns>A string that represents the content of the file or null if the file does not exists or it has no content</returns>
    public static async Task<string?> ReadFromPakAsync(string FileName, string archFile)
    {
        if (!isInitialized) throw new Exception("ArchiveManager is not initialized");
        archFile = archiveFolder + archFile;
        if (!File.Exists(archFile))
            throw new Exception("Failed to load file !");

        try
        {
            string? textValue = null;
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
        catch (Exception ex)
        {
            Config.Logger.Log(message: ex.Message, source: typeof(ArchiveManager), type: LogType.ERROR); // Write the error to a file
            await Task.Delay(100);
            return await ReadFromPakAsync(FileName, archFile);
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
    public static async Task ExtractArchive(
        string zip, string folder, IProgress<float> progress,
        UnzipProgressType type)
    {
        if (!isInitialized) throw new Exception("ArchiveManager is not initialized");
        Directory.CreateDirectory(folder);
        using (var archive = ZipFile.OpenRead(zip))
        {
            if (type == UnzipProgressType.PERCENTAGE_FROM_NUMBER_OF_FILES)
            {
                var totalZIPFiles  = archive.Entries.Count();
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
                            Config.Logger.Log(ex.Message, source: typeof(ArchiveManager), type: LogType.ERROR);
                        }

                    currentZIPFile++;
                    await Task.Delay(10);
                    if (progress != null)
                        progress.Report((float)currentZIPFile / totalZIPFiles * 100);
                }
            }
            else if (type == UnzipProgressType.PERCENTAGE_FROM_TOTAL_SIZE)
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
                        Config.Logger.Log(ex.Message, source: typeof(ArchiveManager), type: LogType.ERROR);
                    }

                    await Task.Delay(10);
                    if (progress != null)
                        progress.Report((float)currentSize / zipSize * 100);
                }
            }
        }
    }
}
