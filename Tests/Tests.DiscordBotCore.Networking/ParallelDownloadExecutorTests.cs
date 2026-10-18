using System.Net;
using DiscordBotCore.Networking;

namespace Tests.DiscordBotCore.Networking;

public class ParallelDownloadExecutorTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-parallel-tests", Guid.NewGuid().ToString("N"));

    public ParallelDownloadExecutorTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private static FakeHttpHandler HandlerWithFiles(int count, int size = 50_000)
    {
        var handler = new FakeHttpHandler();
        for (int i = 0; i < count; i++)
        {
            handler.Add($"https://example.com/{i}.dll", Enumerable.Repeat((byte)i, size).ToArray());
        }

        return handler;
    }

    [Fact]
    public async Task ExecuteAllTasks_DownloadsEveryFileCompletely()
    {
        var handler = HandlerWithFiles(6);
        var executor = new ParallelDownloadExecutor(new HttpClient(handler));
        for (int i = 0; i < 6; i++)
        {
            executor.AddTask($"https://example.com/{i}.dll", Path.Combine(_folder, "deps", $"sub{i}", $"{i}.dll"));
        }

        await executor.ExecuteAllTasks();

        for (int i = 0; i < 6; i++)
        {
            byte[] content = await File.ReadAllBytesAsync(Path.Combine(_folder, "deps", $"sub{i}", $"{i}.dll"));
            Assert.Equal(50_000, content.Length);
            Assert.All(content, b => Assert.Equal((byte)i, b));
        }
    }

    [Fact]
    public async Task AddTask_CreatesTheTargetFolderAndDoesNotStartTheDownload()
    {
        var handler = HandlerWithFiles(1);
        var executor = new ParallelDownloadExecutor(new HttpClient(handler));
        string target = Path.Combine(_folder, "a", "b", "0.dll");

        executor.AddTask("https://example.com/0.dll", target);
        await Task.Delay(50);

        Assert.True(Directory.Exists(Path.GetDirectoryName(target)));
        Assert.Empty(handler.RequestedUrls);
        Assert.False(File.Exists(target));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    public async Task ExecuteAllTasks_LimitsTheParallelDownloads(int limit)
    {
        var handler = HandlerWithFiles(8);
        handler.Delay = TimeSpan.FromMilliseconds(40);
        var executor = new ParallelDownloadExecutor(new HttpClient(handler));
        for (int i = 0; i < 8; i++)
        {
            executor.AddTask($"https://example.com/{i}.dll", Path.Combine(_folder, $"{i}.dll"));
        }

        await executor.ExecuteAllTasks(limit);

        Assert.Equal(8, handler.RequestedUrls.Count);
        Assert.InRange(handler.MaxConcurrentRequests, 1, limit);
    }

    [Fact]
    public async Task ExecuteAllTasks_RunsTheFinishActionOnce()
    {
        var executor = new ParallelDownloadExecutor(new HttpClient(HandlerWithFiles(2)));
        executor.AddTask("https://example.com/0.dll", Path.Combine(_folder, "0.dll"));
        executor.AddTask("https://example.com/1.dll", Path.Combine(_folder, "1.dll"));
        int finished = 0;
        executor.SetFinishAction(() => finished++);

        await executor.ExecuteAllTasks();

        Assert.Equal(1, finished);
    }

    [Fact]
    public async Task ExecuteAllTasks_ReportsProgressPerFile()
    {
        var executor = new ParallelDownloadExecutor(new HttpClient(HandlerWithFiles(1)));
        var done = new TaskCompletionSource();
        executor.AddTask("https://example.com/0.dll", Path.Combine(_folder, "0.dll"), percent =>
        {
            if (percent >= 100f)
            {
                done.TrySetResult();
            }
        });

        await executor.ExecuteAllTasks();

        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task ExecuteAllTasks_FailsWhenADownloadFails()
    {
        var handler = HandlerWithFiles(1);
        var executor = new ParallelDownloadExecutor(new HttpClient(handler));
        executor.AddTask("https://example.com/0.dll", Path.Combine(_folder, "0.dll"));
        executor.AddTask("https://example.com/missing.dll", Path.Combine(_folder, "missing.dll"));

        await Assert.ThrowsAsync<HttpRequestException>(() => executor.ExecuteAllTasks());

        Assert.False(File.Exists(Path.Combine(_folder, "missing.dll")));
    }

    [Fact]
    public async Task StartTasks_RunsTheGivenTasksAndTheDownloads()
    {
        var external = new TaskCompletionSource();
        var executor = new ParallelDownloadExecutor([external.Task]);
        int finished = 0;
        executor.SetFinishAction(() => finished++);

        Task start = executor.StartTasks();
        await Task.Delay(20);
        Assert.False(start.IsCompleted);

        external.SetResult();
        await start;
        Assert.Equal(1, finished);
    }

    [Fact]
    public async Task Downloads_RunOnlyOnce()
    {
        var handler = HandlerWithFiles(1);
        var executor = new ParallelDownloadExecutor(new HttpClient(handler));
        executor.AddTask("https://example.com/0.dll", Path.Combine(_folder, "0.dll"));

        await executor.ExecuteAllTasks();
        await executor.ExecuteAllTasks();

        Assert.Single(handler.RequestedUrls);
    }

    [Theory]
    [InlineData("", "file.dll")]
    [InlineData("https://example.com/0.dll", "")]
    public void AddTask_RejectsEmptyArguments(string link, string location)
    {
        var executor = new ParallelDownloadExecutor();

        Assert.Throws<ArgumentException>(() => executor.AddTask(link, location));
        Assert.Throws<ArgumentException>(() => executor.AddTask(link, location, _ => { }));
    }
}
