using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace DiscordBotCore.Others;

public static class ArchiveManager
{
    
    private static readonly string _ArchivesFolder = "./Data/Archives";

    public static void CreateFromFile(string file, string folder)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var archiveName = folder + Path.GetFileNameWithoutExtension(file) + ".zip";
        if (File.Exists(archiveName))
            File.Delete(archiveName);

        using(ZipArchive archive = ZipFile.Open(archiveName, ZipArchiveMode.Create))
        {
            archive.CreateEntryFromFile(file, Path.GetFileName(file));
        }
    }

    /// <summary>
    /// Read a file from a zip archive. The output is a byte array
    /// </summary>
    /// <param name="fileName">The file name in the archive</param>
    /// <param name="archName">The archive location on the disk</param>
    /// <returns>An array of bytes that represents the Stream value from the file that was read inside the archive</returns>
    public static async Task<byte[]?> ReadAllBytes(string fileName, string archName)
    {
        string? archiveFolderBasePath = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ArchiveFolder", _ArchivesFolder);
        if(archiveFolderBasePath is null)
            throw new Exception("Archive folder not found");

        Directory.CreateDirectory(archiveFolderBasePath);
        
        archName = Path.Combine(archiveFolderBasePath, archName);

        if (!File.Exists(archName))
            throw new Exception("Failed to load file !");

        using var zip   = ZipFile.OpenRead(archName);
        var       entry = zip.Entries.FirstOrDefault(entry => entry.FullName == fileName || entry.Name == fileName);
        if (entry is null) throw new Exception("File not found in archive");

        await using var memoryStream = new MemoryStream();
        var             stream       = entry.Open();
        await stream.CopyToAsync(memoryStream);
        var data = memoryStream.ToArray();

        stream.Close();
        memoryStream.Close();
        
        Console.WriteLine("Read file from archive: " + fileName);
        Console.WriteLine("Size: " + data.Length);

        return data;
    }

    /// <summary>
    ///     Read data from a file that is inside an archive (ZIP format)
    /// </summary>
    /// <param name="fileName">The file name that is inside the archive or its full path</param>
    /// <param name="archFile">The archive location from the PAKs folder</param>
    /// <returns>A string that represents the content of the file or null if the file does not exists or it has no content</returns>
    public static async Task<string?> ReadFromPakAsync(string fileName, string archFile)
    {
        string? archiveFolderBasePath = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ArchiveFolder", _ArchivesFolder);
        if(archiveFolderBasePath is null)
            throw new Exception("Archive folder not found");
        
        Directory.CreateDirectory(archiveFolderBasePath);
        
        archFile = Path.Combine(archiveFolderBasePath, archFile);
        
        if (!File.Exists(archFile))
            throw new Exception("Failed to load file !");

        try
        {
            string? textValue = null;
            using (var fs = new FileStream(archFile, FileMode.Open))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Read))
            {
                foreach (var entry in zip.Entries)
                    if (entry.Name == fileName || entry.FullName == fileName)
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
            Application.Logger.Log(ex.Message, typeof(ArchiveManager), LogType.Error); // Write the error to a file
            await Task.Delay(100);
            return await ReadFromPakAsync(fileName, archFile);
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
        Directory.CreateDirectory(folder);
        using var archive       = ZipFile.OpenRead(zip);
        var       totalZipFiles = archive.Entries.Count();
        if (type == UnzipProgressType.PERCENTAGE_FROM_NUMBER_OF_FILES)
        {
            var currentZipFile = 0;
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
                        Application.Logger.Log(ex.Message, typeof(ArchiveManager), LogType.Error);
                    }

                currentZipFile++;
                await Task.Delay(10);
                if (progress != null)
                    progress.Report((float)currentZipFile / totalZipFiles * 100);
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
                    string path = Path.Combine(folder, entry.FullName);
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    
                    entry.ExtractToFile(path, true);
                    currentSize += (ulong)entry.CompressedLength;
                }
                catch (Exception ex)
                {
                    Application.Logger.Log(ex.Message, typeof(ArchiveManager), LogType.Error);
                }

                await Task.Delay(10);
                if (progress != null)
                    progress.Report((float)currentSize / zipSize * 100);
            }
        }
    }
}
