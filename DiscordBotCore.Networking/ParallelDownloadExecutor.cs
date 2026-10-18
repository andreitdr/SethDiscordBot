namespace DiscordBotCore.Networking;

public class ParallelDownloadExecutor
{
    private readonly List<Task> _listOfTasks;
    // Downloads only start in StartTasks/ExecuteAllTasks, so maxDegreeOfParallelism really limits them
    private readonly List<Func<Task>> _pendingDownloads = new();
    private readonly HttpClient _httpClient;
    private Action? OnFinishAction { get; set; }
    
    public ParallelDownloadExecutor(List<Task> listOfTasks) : this(listOfTasks, new HttpClient())
    {
    }

    public ParallelDownloadExecutor() : this(new List<Task>(), new HttpClient())
    {
    }

    public ParallelDownloadExecutor(HttpClient httpClient) : this(new List<Task>(), httpClient)
    {
    }

    private ParallelDownloadExecutor(List<Task> listOfTasks, HttpClient httpClient)
    {
        _httpClient = httpClient;
        _listOfTasks = listOfTasks;
    }
    
    public async Task StartTasks()
    {
        await Task.WhenAll(_listOfTasks.Concat(TakePendingDownloads().Select(download => download())));
        OnFinishAction?.Invoke();
    }
    
    public async Task ExecuteAllTasks(int maxDegreeOfParallelism = 4)
    {
        using var semaphore = new SemaphoreSlim(maxDegreeOfParallelism);
        
        var downloads = TakePendingDownloads().Select(async download =>
        {
            await semaphore.WaitAsync();
            try
            {
                await download();
            }
            finally
            {
                semaphore.Release();
            }
        });
        
        await Task.WhenAll(_listOfTasks.Concat(downloads));
        OnFinishAction?.Invoke();
    }
    
    public void SetFinishAction(Action action)
    {
        OnFinishAction = action;
    }

    public void AddTask(string downloadLink, string downloadLocation)
    {
        AddDownload(downloadLink, downloadLocation, null);
    }
    
    public void AddTask(string downloadLink, string downloadLocation, Action<float> progressCallback)
    {
        AddDownload(downloadLink, downloadLocation, new Progress<float>(progressCallback));
    }

    private void AddDownload(string downloadLink, string downloadLocation, IProgress<float>? progress)
    {
        if (string.IsNullOrEmpty(downloadLink) || string.IsNullOrEmpty(downloadLocation))
            throw new ArgumentException("Download link or location cannot be null or empty.");

        string? directory = Path.GetDirectoryName(downloadLocation);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        _pendingDownloads.Add(() => FileDownloader.DownloadToFileAsync(_httpClient, downloadLink, downloadLocation, progress));
    }

    private List<Func<Task>> TakePendingDownloads()
    {
        var downloads = _pendingDownloads.ToList();
        _pendingDownloads.Clear();
        return downloads;
    }
}
