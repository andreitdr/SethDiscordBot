using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager.Others
{
    public static class ArchiveManager
    {
        public static bool isInitialized { get; private set; }
        private static string? archiveFolder;

        public static void Initialize()
        {
            if (isInitialized) throw new Exception("ArchiveManager is already initialized");

            if (!Config.Data.ContainsKey("ArchiveFolder"))
                Config.Data["ArchiveFolder"] = "./Data/PAKS/";

            archiveFolder = Config.Data["ArchiveFolder"];

            isInitialized = true;

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
                ex.WriteErrFile(); // Write the error to a file
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
        public static async Task ExtractArchive(string zip, string folder, IProgress<float> progress,
                                                UnzipProgressType type)
        {
            if (!isInitialized) throw new Exception("ArchiveManager is not initialized");
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
                                Config.Logger.Log($"Failed to extract {entry.Name}. Exception: {ex.Message}", "Archive Manager", TextType.ERROR);
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
                            Config.Logger.Log($"Failed to extract {entry.Name}. Exception: {ex.Message}", "Archive Manager", TextType.ERROR);
                        }

                        await Task.Delay(10);
                        if (progress != null)
                            progress.Report((float)currentSize / zipSize * 100);
                    }
                }
            }
        }
    }
}
