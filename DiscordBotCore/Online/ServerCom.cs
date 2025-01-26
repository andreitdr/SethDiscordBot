using System;
using System.Collections.Generic;
using System.IO;

using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DiscordBotCore.Online.Helpers;

namespace DiscordBotCore.Online;

public static class ServerCom
{
    private static async Task DownloadFileAsync(
        string URL, string location, IProgress<float>? progress,
        IProgress<long>? downloadedBytes)
    {
        using (var client = new HttpClient())
        {
            client.Timeout = TimeSpan.FromMinutes(5);
            if(Directory.Exists(Path.GetDirectoryName(location)) == false)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(location));
            }
            using (var file = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await client.DownloadFileAsync(URL, file, progress, downloadedBytes);
            }
        }
    }

    public static async Task DownloadFileAsync(string url, string location, IProgress<float> progress)
    {
        await DownloadFileAsync(url, location, progress, null);
    }
}
