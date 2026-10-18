using DiscordBotCore.Logging;
using WebUI.Services;

namespace Tests.WebUI;

public class LogStreamServiceTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-logstream-tests", Guid.NewGuid().ToString("N"));
    private readonly Logger _logger;

    public LogStreamServiceTests()
    {
        Directory.CreateDirectory(_folder);
        _logger = new Logger(_folder, "{Message}", 100);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    [Fact]
    public void Messages_AreTheLoggersHistory()
    {
        using var stream = new LogStreamService(_logger);
        _logger.Log("one");
        _logger.Log("two");

        Assert.Equal(["one", "two"], stream.Messages.Select(m => m.Message));
    }

    [Fact]
    public async Task ABurstOfMessages_CausesOneUpdate()
    {
        using var stream = new LogStreamService(_logger);
        int changes = 0;
        stream.Changed += () => Interlocked.Increment(ref changes);

        for (int i = 0; i < 50; i++)
        {
            _logger.Log($"message {i}");
        }

        await Task.Delay(500);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task MessagesAfterAnUpdate_CauseAnotherUpdate()
    {
        using var stream = new LogStreamService(_logger);
        int changes = 0;
        stream.Changed += () => Interlocked.Increment(ref changes);

        _logger.Log("first");
        await Task.Delay(400);
        _logger.Log("second");
        await Task.Delay(400);

        Assert.Equal(2, changes);
    }

    [Fact]
    public void Clear_EmptiesTheHistoryAndUpdatesRightAway()
    {
        using var stream = new LogStreamService(_logger);
        _logger.Log("one");
        int changes = 0;
        stream.Changed += () => changes++;

        stream.Clear();

        Assert.Empty(stream.Messages);
        Assert.Equal(1, changes);
    }

    [Fact]
    public async Task Dispose_StopsListening()
    {
        var stream = new LogStreamService(_logger);
        int changes = 0;
        stream.Changed += () => changes++;

        stream.Dispose();
        _logger.Log("after dispose");
        await Task.Delay(400);

        Assert.Equal(0, changes);
    }
}
