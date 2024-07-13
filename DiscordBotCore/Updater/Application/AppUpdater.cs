using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.Updater;
using DiscordBotCore.Online;
using DiscordBotCore.Others;

namespace DiscordBotCore.Updater.Application
{
    public class AppUpdater
    {

        const string ProjectName = "SethDiscordBot";

        private static readonly string _DefaultUpdateUrl         = $"https://github.com/andreitdr/{ProjectName}/releases/latest";
        private static readonly string _DefaultUpdateDownloadUrl = $"https://github.com/andreitdr/{ProjectName}/releases/download/v";
        
        private static readonly string _WindowsUpdateFile = "win-x64.zip";
        private static readonly string _LinuxUpdateFile   = "linux-x64.zip";
        private static readonly string _MacOSUpdateFile   = "osx-x64.zip";
        
        private static readonly string       _TempUpdateFolder = "temp";
        
        private async Task<AppVersion> GetOnlineVersion()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(_DefaultUpdateUrl);

            if (!response.IsSuccessStatusCode)
                return AppVersion.CurrentAppVersion;
            
            var url     = response.RequestMessage.RequestUri.ToString();
            var version = url.Split('/')[^1].Substring(1); // Remove the 'v' from the version number
            
            return new AppVersion(version);
        }
        
        private string GetDownloadUrl(AppVersion version)
        {
            string downloadUrl = _DefaultUpdateDownloadUrl;
            
            downloadUrl += $"{version.ToShortString()}/";
            
            if(OperatingSystem.IsWindows())
                downloadUrl += _WindowsUpdateFile;
            else if (OperatingSystem.IsLinux())
                downloadUrl += _LinuxUpdateFile;
            else if (OperatingSystem.IsMacOS())
                downloadUrl += _MacOSUpdateFile;
            else
                throw new PlatformNotSupportedException("Unsupported operating system");
            
            return downloadUrl;
        }

        public async Task<Update?> PrepareUpdate()
        {
            AppVersion currentVersion = AppVersion.CurrentAppVersion;
            AppVersion newVersion     = await GetOnlineVersion();
            
            if(!newVersion.IsNewerThan(currentVersion))
                return null;
            
            string downloadUrl = GetDownloadUrl(newVersion);
            Update update = new Update(currentVersion, newVersion, downloadUrl);
            
            return update;
        }
        
        private void PrepareCurrentFolderForOverwrite()
        {
            List<string> files = new List<string>();
            files.AddRange(Directory.GetFiles("./"));

            foreach (var file in files)
            {
                File.Move(file, file + ".bak");
            }
        }

        private async Task CopyFolderContentOverCurrentFolder(string current, string otherFolder)
        {
            var files = Directory.GetFiles(otherFolder, "*.*", SearchOption.AllDirectories);
            foreach (var file in files)
            {
                string relativePath = file.Replace(otherFolder, "");
                string newPath = current + relativePath;
                Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                File.Copy(file, newPath);
            }
        }

        public async Task SelfUpdate(Update update, IProgress<float> progress)
        {
            Directory.CreateDirectory(_TempUpdateFolder);

            string tempFile = $"./{_TempUpdateFolder}/update.zip";
            string tempFolder = $"./{_TempUpdateFolder}/";

            Directory.CreateDirectory(tempFolder);
            
            await ServerCom.DownloadFileAsync(update.UpdateUrl, tempFile, progress);

            await ArchiveManager.ExtractArchive(tempFile, tempFolder, progress, UnzipProgressType.PERCENTAGE_FROM_TOTAL_SIZE);
            
            PrepareCurrentFolderForOverwrite();

            if (OperatingSystem.IsWindows())
                tempFolder += _WindowsUpdateFile;
            else if (OperatingSystem.IsLinux())
                tempFolder += _LinuxUpdateFile;
            else if (OperatingSystem.IsMacOS())
                tempFolder += _MacOSUpdateFile;
            else throw new PlatformNotSupportedException();
            
            await CopyFolderContentOverCurrentFolder("./", tempFolder.Substring(0, tempFolder.Length-4)); // Remove the .zip from the folder name

            Process.Start("DiscordBot", "--update-cleanup");
            Environment.Exit(0);
            
        }

    }
}
