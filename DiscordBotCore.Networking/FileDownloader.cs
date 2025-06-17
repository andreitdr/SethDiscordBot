using DiscordBotCore.Networking.Helpers;

namespace DiscordBotCore.Networking;

public class FileDownloader
{
    private readonly string _DownloadUrl;
    private readonly string _DownloadLocation;
    
    private readonly HttpClient _HttpClient;
    
    public FileDownloader(string downloadUrl, string downloadLocation)
    {
        _DownloadUrl = downloadUrl;
        _DownloadLocation = downloadLocation;
        
        _HttpClient = new HttpClient();
    }
    
    public async Task DownloadFile(Action<float> progressCallback)
    {
        await using var fileStream = new FileStream(_DownloadLocation, FileMode.Create, FileAccess.Write, FileShare.None);
        await _HttpClient.DownloadFileAsync(_DownloadUrl, fileStream, new Progress<float>(progressCallback));
    }
}