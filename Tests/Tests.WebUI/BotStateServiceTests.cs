using Discord.WebSocket;
using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Loading;
using DiscordBotCore.Utilities.Responses;
using Moq;
using WebUI.Services;

namespace Tests.WebUI;

/// <summary>The dashboard's bot lifecycle, over a fake bot that can connect, fail or hang.</summary>
public class BotStateServiceTests : IDisposable
{
    private readonly Mock<IDiscordBotApplication> _bot = new();
    private readonly Mock<IPluginLoader> _loader = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly DiscordSocketClient _client = new();
    private bool _ready;
    private bool _pluginsLoaded;
    private int _startCalls;

    public BotStateServiceTests()
    {
        _configuration.Setup(c => c.Get<string>("token")).Returns("token");
        _bot.SetupGet(b => b.IsReady).Returns(() => _ready);
        _bot.SetupGet(b => b.Client).Returns(_client);
        ConnectsImmediately();
        _bot.Setup(b => b.StopAsync()).Returns(() =>
        {
            _ready = false;
            _pluginsLoaded = false;
            return Task.CompletedTask;
        });

        _loader.SetupGet(l => l.IsLoaded).Returns(() => _pluginsLoaded);
        _loader.Setup(l => l.LoadPlugins()).Returns(() =>
        {
            _pluginsLoaded = true;
            return Task.CompletedTask;
        });
        _loader.Setup(l => l.UnloadAllPlugins()).Returns(() =>
        {
            _pluginsLoaded = false;
            return Task.CompletedTask;
        });
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private BotStateService Create(TimeSpan? startTimeout = null) =>
        new(_bot.Object, _loader.Object, _configuration.Object, new Mock<ILogger>().Object)
        {
            StartTimeout = startTimeout ?? TimeSpan.FromSeconds(5)
        };

    private void ConnectsImmediately() =>
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).Returns(() =>
        {
            _startCalls++;
            _ready = true;
            return Task.CompletedTask;
        });

    /// <summary>Never reaches Ready on its own, like a bot with missing intents: it waits until cancelled.</summary>
    private void HangsUntilCancelled() =>
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).Returns<CancellationToken>(token =>
        {
            _startCalls++;
            return Task.Delay(Timeout.Infinite, token);
        });

    #region Starting

    [Fact]
    public async Task Start_WithoutToken_FailsWithoutTrying()
    {
        _configuration.Setup(c => c.Get<string>("token")).Returns((string?)null);
        var service = Create();

        IResponse<bool> response = await service.StartAsync();

        Assert.False(response.IsSuccess);
        Assert.Contains("token", response.Message);
        Assert.Equal(BotStatus.Offline, service.Status);
        Assert.False(service.HasToken);
        Assert.Equal(0, _startCalls);
    }

    [Fact]
    public async Task Start_ConnectsAndAttachesTheClient()
    {
        var service = Create();
        int changes = 0;
        service.Changed += () => changes++;

        IResponse<bool> response = await service.StartAsync();

        Assert.True(response.IsSuccess);
        Assert.True(service.IsRunning);
        Assert.NotNull(service.OnlineSince);
        Assert.Equal(1, _startCalls);
        Assert.True(changes >= 2);
    }

    [Fact]
    public async Task Start_WhenAlreadyRunning_DoesNotStartAgain()
    {
        var service = Create();
        await service.StartAsync();

        IResponse<bool> response = await service.StartAsync();

        Assert.True(response.IsSuccess);
        Assert.Equal(1, _startCalls);
    }

    [Fact]
    public async Task Start_WithARejectedToken_ExplainsIt()
    {
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("401: Unauthorized"));
        var service = Create();

        IResponse<bool> response = await service.StartAsync();

        Assert.False(response.IsSuccess);
        Assert.Equal("Discord rejected the token. Update it in Settings.", response.Message);
        Assert.Equal(BotStatus.Offline, service.Status);
    }

    [Fact]
    public async Task Start_WithAnotherError_ReportsItsMessage()
    {
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).ThrowsAsync(new InvalidOperationException("no network"));
        var service = Create();

        IResponse<bool> response = await service.StartAsync();

        Assert.Equal("no network", response.Message);
        Assert.Equal(BotStatus.Offline, service.Status);
    }

    [Fact]
    public async Task Start_ThatTakesTooLong_ReturnsButKeepsConnecting()
    {
        var ready = new TaskCompletionSource();
        _bot.Setup(b => b.StartAsync(It.IsAny<CancellationToken>())).Returns(async () =>
        {
            await ready.Task;
            _ready = true;
        });
        var service = Create(TimeSpan.FromMilliseconds(100));

        IResponse<bool> response = await service.StartAsync();

        Assert.False(response.IsSuccess);
        Assert.Contains("taking long", response.Message);
        Assert.Equal(BotStatus.Connecting, service.Status);

        ready.SetResult();
        IResponse<bool> late = await service.WaitForPendingStartAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(late.IsSuccess);
        Assert.True(service.IsRunning);
    }

    [Fact]
    public async Task WaitForPendingStart_WithoutAnAttempt_Fails()
    {
        Assert.False((await Create().WaitForPendingStartAsync()).IsSuccess);
    }

    [Fact]
    public async Task Start_WhileAnotherOperationRuns_IsRefused()
    {
        HangsUntilCancelled();
        var service = Create();

        Task<IResponse<bool>> first = service.StartAsync();
        IResponse<bool> second = await service.StartAsync();

        Assert.False(second.IsSuccess);
        Assert.Contains("already in progress", second.Message);
        await service.StopAsync();
        await first.WaitAsync(TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Cancelling a connection attempt

    [Fact]
    public async Task Cancel_WhileStartIsStillWaiting_StopsTheAttempt()
    {
        HangsUntilCancelled();
        var service = Create();
        Task<IResponse<bool>> start = service.StartAsync();
        Assert.Equal(BotStatus.Connecting, service.Status);

        IResponse<bool> cancel = await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));
        IResponse<bool> started = await start.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(cancel.IsSuccess);
        Assert.False(started.IsSuccess);
        Assert.Contains("cancelled", started.Message);
        Assert.Equal(BotStatus.Offline, service.Status);
    }

    [Fact]
    public async Task Cancel_AfterTheTimeout_StopsTheAttempt()
    {
        HangsUntilCancelled();
        var service = Create(TimeSpan.FromMilliseconds(50));
        await service.StartAsync();
        Assert.Equal(BotStatus.Connecting, service.Status);

        IResponse<bool> cancel = await service.StopAsync().WaitAsync(TimeSpan.FromSeconds(5));

        Assert.True(cancel.IsSuccess);
        Assert.Equal(BotStatus.Offline, service.Status);
    }

    [Fact]
    public async Task AfterCancelling_TheBotCanStartAgain()
    {
        HangsUntilCancelled();
        var service = Create(TimeSpan.FromMilliseconds(50));
        await service.StartAsync();
        await service.StopAsync();

        ConnectsImmediately();
        IResponse<bool> retry = await service.StartAsync();

        Assert.True(retry.IsSuccess, retry.Message);
        Assert.Equal(2, _startCalls);
        Assert.True(service.IsRunning);
    }

    #endregion

    #region Stopping

    [Fact]
    public async Task Stop_StopsARunningBot()
    {
        var service = Create();
        await service.StartAsync();

        IResponse<bool> response = await service.StopAsync();

        Assert.True(response.IsSuccess);
        Assert.Equal(BotStatus.Offline, service.Status);
        Assert.Null(service.OnlineSince);
        Assert.Empty(service.Guilds);
        Assert.Null(service.Latency);
        _bot.Verify(b => b.StopAsync(), Times.Once);
    }

    [Fact]
    public async Task Stop_WhenNotRunning_JustReportsOffline()
    {
        var service = Create();

        IResponse<bool> response = await service.StopAsync();

        Assert.True(response.IsSuccess);
        Assert.Equal(BotStatus.Offline, service.Status);
        _bot.Verify(b => b.StopAsync(), Times.Never);
    }

    [Fact]
    public async Task Stop_ThatFails_ReportsTheErrorAndKeepsTheRealStatus()
    {
        var service = Create();
        await service.StartAsync();
        _bot.Setup(b => b.StopAsync()).ThrowsAsync(new InvalidOperationException("stuck"));

        IResponse<bool> response = await service.StopAsync();

        Assert.False(response.IsSuccess);
        Assert.Equal("stuck", response.Message);
        Assert.Equal(BotStatus.Online, service.Status);
    }

    #endregion

    #region Plugins

    [Fact]
    public async Task LoadPlugins_NeedsARunningBot()
    {
        IResponse<bool> response = await Create().LoadPluginsAsync();

        Assert.False(response.IsSuccess);
        Assert.Contains("Start the bot", response.Message);
        _loader.Verify(l => l.LoadPlugins(), Times.Never);
    }

    [Fact]
    public async Task LoadPlugins_LoadsOnce()
    {
        var service = Create();
        await service.StartAsync();

        Assert.True((await service.LoadPluginsAsync()).IsSuccess);
        Assert.True((await service.LoadPluginsAsync()).IsSuccess);

        Assert.True(service.PluginsLoaded);
        Assert.False(service.PluginsBusy);
        _loader.Verify(l => l.LoadPlugins(), Times.Once);
    }

    [Fact]
    public async Task ReloadPlugins_UnloadsThenLoads()
    {
        var service = Create();
        await service.StartAsync();
        await service.LoadPluginsAsync();
        var calls = new List<string>();
        _loader.Setup(l => l.UnloadAllPlugins()).Callback(() => calls.Add("unload")).Returns(Task.CompletedTask);
        _loader.Setup(l => l.LoadPlugins()).Callback(() => calls.Add("load")).Returns(Task.CompletedTask);

        Assert.True((await service.ReloadPluginsAsync()).IsSuccess);

        Assert.Equal(["unload", "load"], calls);
    }

    [Fact]
    public async Task LoadPlugins_ThatThrows_ReportsAFailure()
    {
        var service = Create();
        await service.StartAsync();
        _loader.Setup(l => l.LoadPlugins()).ThrowsAsync(new InvalidOperationException("bad plugin"));

        IResponse<bool> response = await service.LoadPluginsAsync();

        Assert.False(response.IsSuccess);
        Assert.Contains("bad plugin", response.Message);
        Assert.False(service.PluginsBusy);
    }

    [Fact]
    public async Task WithPluginsUnloaded_UnloadsRunsTheActionAndLoadsAgain()
    {
        var service = Create();
        await service.StartAsync();
        await service.LoadPluginsAsync();
        bool loadedDuringAction = true;

        IResponse<bool> response = await service.WithPluginsUnloadedAsync(() =>
        {
            loadedDuringAction = service.PluginsLoaded;
            return Task.FromResult<IResponse<bool>>(Response.Success());
        });

        Assert.True(response.IsSuccess);
        Assert.False(loadedDuringAction);
        Assert.True(service.PluginsLoaded);
    }

    [Fact]
    public async Task WithPluginsUnloaded_WhenNotLoaded_OnlyRunsTheAction()
    {
        var service = Create();

        IResponse<bool> response = await service.WithPluginsUnloadedAsync(() => Task.FromResult<IResponse<bool>>(Response.Failure("nope")));

        Assert.Equal("nope", response.Message);
        _loader.Verify(l => l.UnloadAllPlugins(), Times.Never);
        _loader.Verify(l => l.LoadPlugins(), Times.Never);
    }

    [Fact]
    public async Task Components_AreOnlyListedWhileLoaded()
    {
        var command = new Mock<DiscordBotCore.PluginCore.Interfaces.IDbCommand>().Object;
        _loader.SetupGet(l => l.Commands).Returns([command]);
        var service = Create();

        Assert.Empty(service.Commands);

        await service.StartAsync();
        await service.LoadPluginsAsync();
        Assert.Same(command, Assert.Single(service.Commands));
    }

    #endregion
}
