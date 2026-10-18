using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Loading;
using Moq;

namespace Tests.DiscordBotCore;

/// <summary>
/// Start and stop paths that never reach Ready. The token is empty, so Discord never accepts the session.
/// </summary>
public class DiscordBotApplicationTests
{
    private readonly Mock<ILogger> _logger = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<IPluginLoader> _pluginLoader = new();

    public DiscordBotApplicationTests()
    {
        _configuration.Setup(c => c.Get<string>("token")).Returns(string.Empty);
    }

    private DiscordBotApplication Create() => new(_logger.Object, _configuration.Object, _pluginLoader.Object);

    [Fact]
    public async Task StopAsync_BeforeStart_OnlyWarns()
    {
        var bot = Create();

        await bot.StopAsync();

        Assert.False(bot.IsReady);
        _logger.Verify(l => l.Log(It.Is<string>(m => m.Contains("not yet initialized")), bot, LogType.Warning), Times.Once);
        _pluginLoader.Verify(l => l.UnloadAllPlugins(), Times.Never);
    }

    [Fact]
    public async Task StartAsync_WaitsForReadyUntilCancelled()
    {
        var bot = Create();
        using var cancellation = new CancellationTokenSource();

        Task start = bot.StartAsync(cancellation.Token);
        await Task.Delay(300);
        Assert.False(start.IsCompleted);

        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => start.WaitAsync(TimeSpan.FromSeconds(20)));
        Assert.False(bot.IsReady);
    }

    [Fact]
    public async Task StartAsync_WhenCancelled_TearsTheClientDown()
    {
        var bot = Create();
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bot.StartAsync(cancellation.Token).WaitAsync(TimeSpan.FromSeconds(20)));

        _pluginLoader.Verify(l => l.ClearDiscordClient(), Times.Once);
    }

    [Fact]
    public async Task StartAsync_AfterACancelledStart_CanStartAgain()
    {
        var bot = Create();

        using (var first = new CancellationTokenSource(TimeSpan.FromMilliseconds(200)))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bot.StartAsync(first.Token).WaitAsync(TimeSpan.FromSeconds(20)));
        }

        using (var second = new CancellationTokenSource(TimeSpan.FromMilliseconds(200)))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => bot.StartAsync(second.Token).WaitAsync(TimeSpan.FromSeconds(20)));
        }

        _pluginLoader.Verify(l => l.ClearDiscordClient(), Times.Exactly(2));
        Assert.False(bot.IsReady);
    }

    [Fact]
    public async Task StartAsync_WithAnAlreadyCancelledToken_StopsRightAway()
    {
        var bot = Create();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            bot.StartAsync(new CancellationToken(canceled: true)).WaitAsync(TimeSpan.FromSeconds(20)));

        _pluginLoader.Verify(l => l.ClearDiscordClient(), Times.Once);
    }
}
