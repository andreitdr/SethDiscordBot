using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Loading;
using Discord.WebSocket;
using Moq;
using WebUI.Services;

namespace Tests.WebUI;

/// <summary>Auto-start on launch and stopping on shutdown.</summary>
public class BotLifetimeServiceTests : IDisposable
{
    private readonly Mock<IDiscordBotApplication> _bot = new();
    private readonly Mock<IPluginLoader> _loader = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<ILogger> _logger = new();
    private readonly DiscordSocketClient _client = new();
    private bool _ready;

    public BotLifetimeServiceTests()
    {
        _configuration.Setup(c => c.Get<string>("token")).Returns("token");
        _bot.SetupGet(b => b.IsReady).Returns(() => _ready);
        _bot.SetupGet(b => b.Client).Returns(_client);
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            _ready = true;
            return Task.CompletedTask;
        });
        _bot.Setup(b => b.StopAsync()).Returns(() =>
        {
            _ready = false;
            return Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private void AutoStart(bool enabled) =>
        _configuration.Setup(c => c.Get(BotLifetimeService.AutoStartConfigurationKey, It.IsAny<bool>())).Returns(enabled);

    private (BotLifetimeService Lifetime, BotStateService State) Create(TimeSpan? startTimeout = null)
    {
        var state = new BotStateService(_bot.Object, _loader.Object, _configuration.Object, _logger.Object)
        {
            StartTimeout = startTimeout ?? TimeSpan.FromSeconds(5)
        };
        return (new BotLifetimeService(state, _configuration.Object, _logger.Object), state);
    }

    private static async Task RunStartupAsync(BotLifetimeService lifetime)
    {
        await lifetime.StartAsync(CancellationToken.None);
        await lifetime.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public async Task WithoutAutoStart_TheBotStaysOffline()
    {
        AutoStart(false);
        var (lifetime, state) = Create();

        await RunStartupAsync(lifetime);

        Assert.Equal(BotStatus.Offline, state.Status);
        _bot.Verify(b => b.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task WithAutoStart_StartsTheBotAndLoadsThePlugins()
    {
        AutoStart(true);
        var (lifetime, state) = Create();

        await RunStartupAsync(lifetime);

        Assert.True(state.IsRunning);
        _loader.Verify(l => l.LoadPlugins(), Times.Once);
    }

    [Fact]
    public async Task WithAutoStart_ButNoToken_OnlyWarns()
    {
        AutoStart(true);
        _configuration.Setup(c => c.Get<string>("token")).Returns(string.Empty);
        var (lifetime, _) = Create();

        await RunStartupAsync(lifetime);

        _bot.Verify(b => b.StartAsync(It.IsAny<CancellationToken>()), Times.Never);
        _logger.Verify(l => l.Log(It.Is<string>(m => m.Contains("no bot token")), lifetime, LogType.Warning), Times.Once);
    }

    [Fact]
    public async Task WithAutoStart_ThatFailsToConnect_DoesNotLoadPlugins()
    {
        AutoStart(true);
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("401"));
        var (lifetime, state) = Create();

        await RunStartupAsync(lifetime);

        Assert.Equal(BotStatus.Offline, state.Status);
        _loader.Verify(l => l.LoadPlugins(), Times.Never);
    }

    [Fact]
    public async Task WithAutoStart_WaitsForASlowConnection()
    {
        AutoStart(true);
        var ready = new TaskCompletionSource();
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await ready.Task;
            _ready = true;
        });
        var (lifetime, state) = Create(TimeSpan.FromMilliseconds(50));

        await lifetime.StartAsync(CancellationToken.None);
        await Task.Delay(200);
        Assert.Equal(BotStatus.Connecting, state.Status);
        ready.SetResult();
        await lifetime.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.True(state.IsRunning);
        _loader.Verify(l => l.LoadPlugins(), Times.Once);
    }

    [Fact]
    public async Task Shutdown_StopsARunningBot()
    {
        AutoStart(true);
        var (lifetime, state) = Create();
        await RunStartupAsync(lifetime);

        await lifetime.StopAsync(CancellationToken.None);

        Assert.Equal(BotStatus.Offline, state.Status);
        _bot.Verify(b => b.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task Shutdown_LeavesAnOfflineBotAlone()
    {
        AutoStart(false);
        var (lifetime, _) = Create();
        await RunStartupAsync(lifetime);

        await lifetime.StopAsync(CancellationToken.None);

        _bot.Verify(b => b.StopAsync(), Times.Never);
    }
}
