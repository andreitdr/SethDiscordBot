using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using DiscordBotCore.Others;

namespace DiscordBotCore.Online.Helpers;

internal static class OnlineFunctions
{

    /// <summary>
    ///     Copy one Stream to another <see langword="async" />
    /// </summary>
    /// <param name="stream">The base stream</param>
    /// <param name="destination">The destination stream</param>
    /// <param name="bufferSize">The buffer to read</param>
    /// <param name="progress">The progress</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <exception cref="ArgumentNullException">Triggered if any <see cref="Stream" /> is empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Triggered if <paramref name="bufferSize" /> is less then or equal to 0</exception>
    /// <exception cref="InvalidOperationException">Triggered if <paramref name="stream" /> is not readable</exception>
    /// <exception cref="ArgumentException">Triggered in <paramref name="destination" /> is not writable</exception>
    public static async Task CopyToOtherStreamAsync(
        this Stream stream, Stream destination, int bufferSize,
        IProgress<long>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (stream == null) throw new ArgumentNullException(nameof(stream));
        if (destination == null) throw new ArgumentNullException(nameof(destination));
        if (bufferSize <= 0) throw new ArgumentOutOfRangeException(nameof(bufferSize));
        if (!stream.CanRead) throw new InvalidOperationException("The stream is not readable.");
        if (!destination.CanWrite)
            throw new ArgumentException("Destination stream is not writable", nameof(destination));

        var buffer = new byte[bufferSize];
        long totalBytesRead = 0;
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)
                                        .ConfigureAwait(false)) != 0)
        {
            await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken).ConfigureAwait(false);
            totalBytesRead += bytesRead;
            progress?.Report(totalBytesRead);
        }
    }


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
        this HttpClient client, string url, Stream destination,
        IProgress<float>? progress = null,
        IProgress<long>? downloadedBytes = null, int bufferSize = 81920,
        CancellationToken cancellation = default)
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
                    if (!contentLength.HasValue)
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

    internal static async Task<bool> IsInternetConnected()
    {
        bool result = false;
        try
        {
            using var client = new HttpClient();
            await client.GetStringAsync("https://www.google.com");
            result = true;
        }
        catch
        {
            result = false;
        }
        
        return result;
    }
}
