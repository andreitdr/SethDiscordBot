using System.Net;
using DiscordBotCore.Networking;

namespace Tests.DiscordBotCore.Networking;

public class FileDownloaderTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-download-tests", Guid.NewGuid().ToString("N"));

    public FileDownloaderTests()
    {
        Directory.CreateDirectory(_folder);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private static byte[] Bytes(int count) => Enumerable.Range(0, count).Select(i => (byte)i).ToArray();

    [Fact]
    public async Task DownloadFile_WritesTheBodyToTheFile()
    {
        byte[] body = Bytes(300_000);
        var handler = new FakeHttpHandler().Add("https://example.com/plugin.dll", body);
        string target = Path.Combine(_folder, "plugin.dll");

        await new FileDownloader("https://example.com/plugin.dll", target, new HttpClient(handler)).DownloadFile(_ => { });

        Assert.Equal(body, await File.ReadAllBytesAsync(target));
    }

    [Fact]
    public async Task DownloadFile_ReportsProgressUpTo100()
    {
        var handler = new FakeHttpHandler().Add("https://example.com/plugin.dll", Bytes(300_000));
        var reports = new List<float>();
        var done = new TaskCompletionSource();

        await new FileDownloader("https://example.com/plugin.dll", Path.Combine(_folder, "plugin.dll"), new HttpClient(handler))
            .DownloadFile(percent =>
            {
                lock (reports)
                {
                    reports.Add(percent);
                }

                if (percent >= 100f)
                {
                    done.TrySetResult();
                }
            });

        // Progress<T> reports on the thread pool, so wait for the last report
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
        lock (reports)
        {
            Assert.Contains(100f, reports);
            Assert.All(reports, percent => Assert.InRange(percent, 0f, 100f));
        }
    }

    [Fact]
    public async Task DownloadFile_WithoutContentLength_StillDownloadsAndReports100()
    {
        byte[] body = Bytes(1000);
        var handler = new FakeHttpHandler().Add("https://example.com/plugin.dll", body, sendLength: false);
        string target = Path.Combine(_folder, "plugin.dll");
        var done = new TaskCompletionSource();

        await new FileDownloader("https://example.com/plugin.dll", target, new HttpClient(handler))
            .DownloadFile(percent =>
            {
                if (percent >= 100f)
                {
                    done.TrySetResult();
                }
            });

        Assert.Equal(body, await File.ReadAllBytesAsync(target));
        await done.Task.WaitAsync(TimeSpan.FromSeconds(5));
    }

    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.Unauthorized)]
    public async Task DownloadFile_OnAnErrorStatus_ThrowsAndLeavesNoFile(HttpStatusCode status)
    {
        var handler = new FakeHttpHandler().Add("https://example.com/plugin.dll", "<html>error</html>"u8.ToArray(), status);
        string target = Path.Combine(_folder, "plugin.dll");

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            new FileDownloader("https://example.com/plugin.dll", target, new HttpClient(handler)).DownloadFile(_ => { }));

        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task DownloadFile_ReplacesAnExistingFile()
    {
        string target = Path.Combine(_folder, "plugin.dll");
        await File.WriteAllBytesAsync(target, Bytes(5000));
        var handler = new FakeHttpHandler().Add("https://example.com/plugin.dll", [1, 2, 3]);

        await new FileDownloader("https://example.com/plugin.dll", target, new HttpClient(handler)).DownloadFile(_ => { });

        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(target));
    }
}
