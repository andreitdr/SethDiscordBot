using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PluginManager.Others;

namespace PluginManager.Online.Helpers;

internal static class OnlineFunctions
{
    /// <summary>
    ///     Downloads a <see cref="Stream" /> and saves it to another <see cref="Stream" />.
    /// </summary>
    /// <param name="client">The <see cref="HttpClient" /> that is used to download the file</param>
    /// <param name="url">The url to the file</param>
    /// <param name="destination">The <see cref="Stream" /> to save the downloaded data</param>
    /// <param name="progress">The <see cref="IProgress{T}" /> that is used to track the download progress</param>
    /// <param name="cancellation">The cancellation token</param>
    /// <returns></returns>
    internal static async Task DownloadFileAsync(
        this HttpClient   client, string url, Stream destination,
        IProgress<float>? progress        = null,
        IProgress<long>?  downloadedBytes = null, int bufferSize = 81920,
        CancellationToken cancellation    = default)
    {
        using (var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellation))
        {
            var contentLength = response.Content.Headers.ContentLength;

            using (var download = await response.Content.ReadAsStreamAsync(cancellation))
            {
                // Ignore progress reporting when no progress reporter was 
                // passed or when the content length is unknown
                if (progress == null || !contentLength.HasValue)
                {
                    await download.CopyToAsync(destination, cancellation);
                    if(!contentLength.HasValue)
                        progress?.Report(100f);
                    return;
                }

                // Convert absolute progress (bytes downloaded) into relative progress (0% - 100%)
                // total ... 100%
                // downloaded ... x%
                // x = downloaded * 100 / total => x = downloaded / total * 100
                var relativeProgress = new Progress<long>(totalBytesDownloaded =>
                    {
                        progress?.Report(totalBytesDownloaded / (float)contentLength.Value * 100);
                        downloadedBytes?.Report(totalBytesDownloaded);
                    }
                );

                // Use extension method to report progress while downloading
                await download.CopyToOtherStreamAsync(destination, bufferSize, relativeProgress, cancellation);
                progress.Report(100f);
            }
        }
    }

    /// <summary>
    ///     Read contents of a file as string from specified URL
    /// </summary>
    /// <param name="url">The URL to read from</param>
    /// <param name="cancellation">The cancellation token</param>
    /// <returns></returns>
    internal static async Task<string> DownloadStringAsync(string url, CancellationToken cancellation = default)
    {
        using var client = new HttpClient();
        return await client.GetStringAsync(url, cancellation);
    }
}
