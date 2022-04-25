using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using PluginManager.Others;

namespace PluginManager.Online.Helpers
{
    internal static class OnlineFunctions
    {
        internal static async Task DownloadFileAsync(this HttpClient client, string url, Stream destination, IProgress<float> progress = null, CancellationToken cancellation = default)
        {
            using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead))
            {
                var contentLength = response.Content.Headers.ContentLength;

                using (var download = await response.Content.ReadAsStreamAsync())
                {

                    // Ignore progress reporting when no progress reporter was 
                    // passed or when the content length is unknown
                    if (progress == null || !contentLength.HasValue)
                    {
                        await download.CopyToAsync(destination);
                        return;
                    }

                    // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                    var relativeProgress = new Progress<long>(totalBytes => progress.Report((float)totalBytes / contentLength.Value * 100));
                    // Use extension method to report progress while downloading
                    await download.CopyToOtherStreamAsync(destination, 81920, relativeProgress, cancellation);
                    progress.Report(1);
                }
            }
        }

        internal static async Task<string> DownloadStringAsync(string url, CancellationToken cancellation = default)
        {
            using (var client = new HttpClient())
            {
                return await client.GetStringAsync(url);
            }
        }
    }
}
