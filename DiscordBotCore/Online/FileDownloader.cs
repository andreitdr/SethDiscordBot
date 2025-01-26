using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace DiscordBotCore.Online;

public class FileDownloader
{
    private readonly HttpClient _httpClient;
    public List<Task> ListOfDownloadTasks { get; private set; }
    private bool IsParallelDownloader { get; set; }
    
    public Action FinishAction { get; private set; }
    
    public FileDownloader(bool isParallelDownloader)
    {
        _httpClient = new HttpClient();
        ListOfDownloadTasks = new List<Task>();
        
        IsParallelDownloader = isParallelDownloader;
    }
    
    public FileDownloader(bool isParallelDownloader, Action finishAction)
    {
        _httpClient = new HttpClient();
        ListOfDownloadTasks = new List<Task>();
        
        IsParallelDownloader = isParallelDownloader;
        FinishAction = finishAction;
    }
    
    public async Task StartDownloadTasks()
    {
        if (IsParallelDownloader)
        {
            await Task.WhenAll(ListOfDownloadTasks);
        }
        else
        {
            foreach (var task in ListOfDownloadTasks)
            {
                await task;
            }
        }
        
        FinishAction?.Invoke();
    }
    
    public void AppendDownloadTask(string downloadLink, string downloadLocation, IProgress<float> progress)
    {
        ListOfDownloadTasks.Add(CreateDownloadTask(_httpClient, downloadLink, downloadLocation, progress));
    }
    
    public void AppendDownloadTask(string downloadLink, string downloadLocation, Action<float> progressCallback)
    {
        ListOfDownloadTasks.Add(CreateDownloadTask(_httpClient, downloadLink, downloadLocation, new Progress<float>(progressCallback)));
    }
    
    public static async Task CreateDownloadTask(HttpClient client, string url, string targetPath, IProgress<float> progress)
    {
        using var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1L;
        var receivedBytes = 0L;
        
        var targetDirectory = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDirectory))
        {
            Directory.CreateDirectory(targetDirectory);
        }

        using var contentStream = await response.Content.ReadAsStreamAsync();
        using var fileStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        var buffer = new byte[8192];
        int bytesRead;

        while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead));
            receivedBytes += bytesRead;
            
            if (totalBytes > 0)
            {
                float calculatedProgress = (float)receivedBytes / totalBytes;
                progress.Report(calculatedProgress);
            }
        }
        
        progress.Report(1f);
    }
}