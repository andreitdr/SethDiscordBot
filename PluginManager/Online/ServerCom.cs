using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

using PluginManager.Online.Helpers;
using PluginManager.Others;

namespace PluginManager.Online;

public static class ServerCom
{
    /// <summary>
    ///     Read all lines from a file async
    /// </summary>
    /// <param name="link">The link of the file</param>
    /// <returns></returns>
    public static async Task<List<string>> ReadTextFromURL(string link)
    {
        var response = await OnlineFunctions.DownloadStringAsync(link);
        var lines = response.Split('\n');
        return lines.ToList();
    }

    /// <summary>
    ///     Download file from url
    /// </summary>
    /// <param name="URL">The url to the file</param>
    /// <param name="location">The location where to store the downloaded data</param>
    /// <param name="progress">The <see cref="IProgress{T}" /> to track the download</param>
    /// <returns></returns>
    public static async Task DownloadFileAsync(string URL, string location, IProgress<float> progress,
                                               IProgress<long>? downloadedBytes = null)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(5);

            using (var file = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await client.DownloadFileAsync(URL, file, progress, downloadedBytes);
            }
        }
    }

    /// <summary>
    ///     Download file from url
    /// </summary>
    /// <param name="URL">The url to the file</param>
    /// <param name="location">The location where to store the downloaded data</param>
    /// <returns></returns>
    public static async Task DownloadFileAsync(string URL, string location)
    {
        var isDownloading = true;
        float c_progress = 0;

        var pbar = new Utilities.ProgressBar(ProgressBarType.NORMAL) { Max = 100f, NoColor = true };

        IProgress<float> progress = new Progress<float>(percent => { c_progress = percent; });


        var updateProgressBarTask = new Task(() =>
            {
                while (isDownloading)
                {
                    pbar.Update(c_progress);
                    if (c_progress == 100f)
                        break;
                    Thread.Sleep(500);
                }
            }
        );

        new Thread(updateProgressBarTask.Start).Start();
        await DownloadFileAsync(URL, location, progress);


        c_progress = pbar.Max;
        pbar.Update(100f);
        isDownloading = false;
    }

    public static async Task DownloadFileNoProgressAsync(string URL, string location)
    {
        IProgress<float> progress = new Progress<float>();
        await DownloadFileAsync(URL, location, progress);
    }

    public static VersionString? GetVersionOfPackage(string pakName)
    {
        if (Config.Plugins.GetVersion(pakName) is null)
            return null;
        return new VersionString(Config.Plugins.GetVersion(pakName));
    }

    public static async Task<VersionString?> GetVersionOfPackageFromWeb(string pakName)
    {
        var url = "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/Versions";
        var data = await ReadTextFromURL(url);
        foreach (var item in data)
        {
            if (item.StartsWith("#"))
                continue;

            string[] split = item.Split(',');
            if (split[0] == pakName)
                return new VersionString(split[1]);
        }
        return null;
    }
}