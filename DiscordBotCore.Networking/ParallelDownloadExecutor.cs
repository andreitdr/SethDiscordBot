using DiscordBotCore.Networking.Helpers;

namespace DiscordBotCore.Networking;

public class ParallelDownloadExecutor
{
    private readonly List<Task> _listOfTasks;
    private readonly HttpClient _httpClient;
    private Action? OnFinishAction { get; set; }
    
    public ParallelDownloadExecutor(List<Task> listOfTasks)
    {
        _httpClient = new HttpClient();
        _listOfTasks = listOfTasks;
    }

    public ParallelDownloadExecutor()
    {
        _httpClient = new HttpClient();
        _listOfTasks = new List<Task>();
    }
    
    public async Task StartTasks()
    {
        await Task.WhenAll(_listOfTasks);
        OnFinishAction?.Invoke();
    }
    
    public async Task ExecuteAllTasks(int maxDegreeOfParallelism = 4)
    {
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        
        var tasks = _listOfTasks.Select(async task =>
        {
            await semaphore.WaitAsync();
            try
            {
                await task;
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(tasks);
        OnFinishAction?.Invoke();
    }
    
    public void SetFinishAction(Action action)
    {
        OnFinishAction = action;
    }

    public void AddTask(string downloadLink, string downloadLocation)
    {
        if (string.IsNullOrEmpty(downloadLink) || string.IsNullOrEmpty(downloadLocation))
            throw new ArgumentException("Download link or location cannot be null or empty.");
        
        if (Directory.Exists(Path.GetDirectoryName(downloadLocation)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(downloadLocation));
        }
        
        var task = CreateDownloadTask(downloadLink, downloadLocation, null);
        _listOfTasks.Add(task);
    }
    
    public void AddTask(string downloadLink, string downloadLocation, Action<float> progressCallback)
    {
        if (string.IsNullOrEmpty(downloadLink) || string.IsNullOrEmpty(downloadLocation))
            throw new ArgumentException("Download link or location cannot be null or empty.");
        
        if (Directory.Exists(Path.GetDirectoryName(downloadLocation)) == false)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(downloadLocation));
        }
        
        var task = CreateDownloadTask(downloadLink, downloadLocation, new Progress<float>(progressCallback));
        _listOfTasks.Add(task);
    }
    
    private Task CreateDownloadTask(string downloadLink, string downloadLocation, IProgress<float> progress)
    {
        var fileStream = new FileStream(downloadLocation, FileMode.Create, FileAccess.Write, FileShare.None);
        return _httpClient.DownloadFileAsync(downloadLink, fileStream, progress);
    }
    
}