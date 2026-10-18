using DiscordBotCore.Networking.Helpers;

namespace DiscordBotCore.Networking;

public class FileDownloader
{
    private readonly string _DownloadUrl;
    private readonly string _DownloadLocation;
    
    private readonly HttpClient _HttpClient;
    
    public FileDownloader(string downloadUrl, string downloadLocation) : this(downloadUrl, downloadLocation, new HttpClient())
    {
    }

    public FileDownloader(string downloadUrl, string downloadLocation, HttpClient httpClient)
    {
        _DownloadUrl = downloadUrl;
        _DownloadLocation = downloadLocation;
        
        _HttpClient = httpClient;
    }
    
    /// <summary>
    /// Downloads the file. An HTTP error status throws <see cref="HttpRequestException"/>, and a failed download
    /// doesn't leave a partial file behind.
    /// </summary>
    public Task DownloadFile(Action<float> progressCallback) =>
        DownloadToFileAsync(_HttpClient, _DownloadUrl, _DownloadLocation, new Progress<float>(progressCallback));

    internal static async Task DownloadToFileAsync(HttpClient httpClient, string url, string location, IProgress<float>? progress)
    {
        try
        {
            await using var fileStream = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.None);
            await httpClient.DownloadFileAsync(url, fileStream, progress);
        }
        catch
        {
            try
            {
                File.Delete(location);
            }
            catch (IOException)
            {
            }

            throw;
        }
    }
}
