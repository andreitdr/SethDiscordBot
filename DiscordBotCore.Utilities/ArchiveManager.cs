using System.IO.Compression;
using DiscordBotCore.Logging;
using DiscordBotCore.Configuration;

namespace DiscordBotCore.Utilities;

public class ArchiveManager
{
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;
    
    public ArchiveManager(ILogger logger, IConfiguration configuration)
    {
        _Logger = logger;
        _Configuration = configuration;
    }

    public void CreateFromFile(string file, string folder)
    {
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        var archiveName = Path.Combine(folder, Path.GetFileNameWithoutExtension(file) + ".zip");
        if (File.Exists(archiveName))
            File.Delete(archiveName);

        using ZipArchive archive = ZipFile.Open(archiveName, ZipArchiveMode.Create);
        archive.CreateEntryFromFile(file, Path.GetFileName(file));
    }

    /// <summary>
    /// Read a file from a zip archive. The output is a byte array
    /// </summary>
    /// <param name="fileName">The file name in the archive</param>
    /// <param name="archName">The archive location on the disk</param>
    /// <returns>An array of bytes that represents the Stream value from the file that was read inside the archive</returns>
    public async Task<byte[]?> ReadAllBytes(string fileName, string archName)
    {
        string? archiveFolderBasePath = _Configuration.Get<string>("ArchiveFolder");
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
    public async Task<string?> ReadFromPakAsync(string fileName, string archFile)
    {
        string? archiveFolderBasePath = _Configuration.Get<string>("ArchiveFolder");
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
            // A corrupt archive stays corrupt: retrying would never end
            _Logger.LogException(ex, this);
            throw;
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
    public async Task ExtractArchive(
        string zip, string folder, IProgress<float> progress,
        UnzipProgressType type)
    {
        Directory.CreateDirectory(folder);
        using var archive = ZipFile.OpenRead(zip);

        int totalFiles = archive.Entries.Count;
        long totalSize = archive.Entries.Sum(entry => entry.CompressedLength);
        int extractedFiles = 0;
        long extractedSize = 0;

        foreach (var entry in archive.Entries)
        {
            ExtractEntry(entry, folder);

            extractedFiles++;
            extractedSize += entry.CompressedLength;
            await Task.Delay(10);

            float percent = type == UnzipProgressType.PercentageFromNumberOfFiles
                ? (float)extractedFiles / totalFiles * 100
                : totalSize == 0 ? 100f : (float)extractedSize / totalSize * 100;
            progress?.Report(percent);
        }
    }

    private void ExtractEntry(ZipArchiveEntry entry, string folder)
    {
        string? destination = GetSafeDestination(folder, entry.FullName);
        if (destination is null)
        {
            _Logger.Log($"Skipped archive entry {entry.FullName}: it points outside {folder}.", this, LogType.Warning);
            return;
        }

        try
        {
            if (entry.FullName.EndsWith('/'))
            {
                Directory.CreateDirectory(destination);
                return;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            entry.ExtractToFile(destination, true);
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
        }
    }

    /// <summary>
    /// Where an entry goes inside <paramref name="folder"/>, or null for names such as <c>../file</c> that would land
    /// outside it ("zip slip").
    /// </summary>
    internal static string? GetSafeDestination(string folder, string entryName)
    {
        string root = Path.GetFullPath(folder);
        string rootWithSeparator = Path.EndsInDirectorySeparator(root) ? root : root + Path.DirectorySeparatorChar;
        string destination = Path.GetFullPath(Path.Combine(root, entryName));

        return destination.StartsWith(rootWithSeparator, StringComparison.Ordinal) ? destination : null;
    }
}
