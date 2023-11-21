using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using PluginManager.Online.Helpers;

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
        var lines    = response.Split('\n');
        return lines.ToList();
    }

    /// <summary>
    ///     Download file from url
    /// </summary>
    /// <param name="URL">The url to the file</param>
    /// <param name="location">The location where to store the downloaded data</param>
    /// <param name="progress">The <see cref="IProgress{T}" /> to track the download</param>
    /// <returns></returns>
    public static async Task DownloadFileAsync(
        string URL, string location, IProgress<float> progress,
        IProgress<long>? downloadedBytes)
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

    public static async Task DownloadFileAsync(string URl, string location, IProgress<float> progress)
    {
        await DownloadFileAsync(URl, location, progress, null);
    }

    public static Task CreateDownloadTask(string URl, string location)
    {
        return DownloadFileAsync(URl, location, null, null);
    }

    public static Task CreateDownloadTask(string URl, string location, IProgress<float> progress)
    {
        return DownloadFileAsync(URl, location, progress, null);
    }

}
