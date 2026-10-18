using Discord;
using Discord.WebSocket;
using DiscordBotCore.Bot;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.PluginManagement.Loading;
using DiscordBotCore.Utilities.Responses;
using IConfiguration = DiscordBotCore.Configuration.IConfiguration;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Services;

/// <summary>
/// The single owner of the bot lifecycle for the WebUI. Every panel and every browser tab goes through this service,
/// so the status they show is always the same and concurrent clicks can't start, stop or load plugins twice.
/// </summary>
public sealed class BotStateService : IStateNotifier, IDisposable
{
    private const int LatencyHistorySize = 40;

    private readonly IDiscordBotApplication _Bot;
    private readonly IPluginLoader _PluginLoader;
    private readonly IConfiguration _Configuration;
    private readonly ILogger _Logger;

    private readonly SemaphoreSlim _Gate = new(1, 1);
    private readonly object _LatencyLock = new();
    private readonly Queue<int> _LatencyHistory = new();

    private DiscordSocketClient? _Client;
    private Task<IResponse<bool>>? _PendingStart;
    private CancellationTokenSource? _StartCancellation;

    public event Action? Changed;

    public BotStateService(IDiscordBotApplication bot, IPluginLoader pluginLoader, IConfiguration configuration, ILogger logger)
    {
        _Bot = bot;
        _PluginLoader = pluginLoader;
        _Configuration = configuration;
        _Logger = logger;
    }

    /// <summary>How long <see cref="StartAsync"/> waits for Ready before returning; the attempt goes on after that.</summary>
    internal TimeSpan StartTimeout { get; init; } = TimeSpan.FromSeconds(30);

    public BotStatus Status { get; private set; } = BotStatus.Offline;
    public DateTime? OnlineSince { get; private set; }
    public bool PluginsLoaded => _PluginLoader.IsLoaded;
    public bool PluginsBusy { get; private set; }

    public bool IsRunning => Status is BotStatus.Online or BotStatus.Reconnecting;
    public bool IsTransitioning => Status is BotStatus.Connecting or BotStatus.Stopping;
    public bool HasToken => !string.IsNullOrWhiteSpace(_Configuration.Get<string>("token"));

    public IReadOnlyList<IDbCommand> Commands => PluginsLoaded ? _PluginLoader.Commands.ToArray() : [];
    public IReadOnlyList<IDbEvent> Events => PluginsLoaded ? _PluginLoader.Events.ToArray() : [];
    public IReadOnlyList<IDbSlashCommand> SlashCommands => PluginsLoaded ? _PluginLoader.SlashCommands.ToArray() : [];

    public int? Latency => IsRunning ? _Client?.Latency : null;

    public IReadOnlyList<int> LatencyHistory
    {
        get
        {
            lock (_LatencyLock)
            {
                return _LatencyHistory.ToArray();
            }
        }
    }

    public IReadOnlyList<GuildSummary> Guilds =>
        _Client is { } client && IsRunning
            ? client.Guilds
                .Select(g => new GuildSummary(g.Id, g.Name, g.IconUrl, g.MemberCount))
                .OrderByDescending(g => g.MemberCount)
                .ToArray()
            : [];

    public string? BotUserName => IsRunning ? _Client?.CurrentUser?.Username : null;

    public async Task<IResponse<bool>> StartAsync()
    {
        if (!await _Gate.WaitAsync(0))
        {
            return Response.Failure("Another bot operation is already in progress.");
        }

        try
        {
            if (_Bot.IsReady)
            {
                AttachClient(_Bot.Client);
                return Response.Success();
            }

            if (_PendingStart is { IsCompleted: false })
            {
                return Response.Failure("The bot is still connecting.");
            }

            if (!HasToken)
            {
                return Response.Failure("Set a bot token in Settings first.");
            }

            SetStatus(BotStatus.Connecting);

            var cancellation = new CancellationTokenSource();
            _StartCancellation = cancellation;
            Task<IResponse<bool>> start = RunStartAsync(cancellation);
            _PendingStart = start;

            // The core StartAsync only returns once the gateway is Ready, so wait for it with a timeout instead of
            // freezing the UI in "Connecting". The attempt goes on in the background until it succeeds or is cancelled.
            if (await Task.WhenAny(start, Task.Delay(StartTimeout)) != start)
            {
                _Logger.Log("The bot hasn't reached Ready yet. It keeps trying in the background.", this, LogType.Warning);
                return Response.Failure("The bot is taking long to connect. Check the token, the privileged intents and your network, or cancel.");
            }

            return await start;
        }
        finally
        {
            _Gate.Release();
        }
    }

    /// <summary>
    /// Waits for a connection attempt that outlived <see cref="StartAsync"/>'s timeout to succeed, fail or be cancelled.
    /// </summary>
    public Task<IResponse<bool>> WaitForPendingStartAsync() =>
        _PendingStart ?? Task.FromResult<IResponse<bool>>(Response.Failure("The bot isn't connecting."));

    public async Task<IResponse<bool>> StopAsync()
    {
        // Cancelling a connection attempt doesn't wait for the gate: StartAsync holds it while it waits for Ready.
        if (Status == BotStatus.Connecting && _StartCancellation is { } cancellation && _PendingStart is { } pendingStart)
        {
            cancellation.Cancel();
            // The attempt tears the client down and sets the status; if it ended another way, it reported that already.
            await pendingStart;
            return Response.Success();
        }

        if (!await _Gate.WaitAsync(0))
        {
            return Response.Failure("Another bot operation is already in progress.");
        }

        try
        {
            if (!_Bot.IsReady)
            {
                SetStatus(BotStatus.Offline);
                return Response.Success();
            }

            SetStatus(BotStatus.Stopping);
            DetachClient();

            // The core StopAsync unloads the plugins before logging out.
            await _Bot.StopAsync();
            _Logger.Log("Application stopped", this);
            return Response.Success();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            return Response.Failure(ex.Message);
        }
        finally
        {
            SetStatus(_Bot.IsReady ? BotStatus.Online : BotStatus.Offline);
            _Gate.Release();
        }
    }

    /// <summary>Re-renders everything that depends on configuration, such as whether a token is set.</summary>
    public void NotifySettingsChanged() => Changed?.Invoke();

    public Task<IResponse<bool>> LoadPluginsAsync() => RunPluginOperation(reload: false);

    public Task<IResponse<bool>> ReloadPluginsAsync() => RunPluginOperation(reload: true);

    /// <summary>
    /// Unloads the plugins (if loaded), runs <paramref name="action"/> and loads them back.
    /// Used when a plugin's files or enabled state change while the bot is running.
    /// </summary>
    public async Task<IResponse<bool>> WithPluginsUnloadedAsync(Func<Task<IResponse<bool>>> action)
    {
        await _Gate.WaitAsync();
        bool wasLoaded = PluginsLoaded;
        try
        {
            SetPluginsBusy(true);
            if (wasLoaded)
            {
                await _PluginLoader.UnloadAllPlugins();
            }

            return await action();
        }
        finally
        {
            if (wasLoaded && _Bot.IsReady)
            {
                await LoadPluginsCore();
            }

            SetPluginsBusy(false);
            _Gate.Release();
        }
    }

    private async Task<IResponse<bool>> RunPluginOperation(bool reload)
    {
        if (!_Bot.IsReady)
        {
            return Response.Failure("Start the bot before loading plugins.");
        }

        if (!await _Gate.WaitAsync(0))
        {
            return Response.Failure("Another bot operation is already in progress.");
        }

        try
        {
            SetPluginsBusy(true);
            if (PluginsLoaded)
            {
                if (!reload)
                {
                    return Response.Success();
                }

                await _PluginLoader.UnloadAllPlugins();
            }

            return await LoadPluginsCore();
        }
        finally
        {
            SetPluginsBusy(false);
            _Gate.Release();
        }
    }

    private async Task<IResponse<bool>> LoadPluginsCore()
    {
        try
        {
            _Logger.Log("Loading plugins", this);
            await _PluginLoader.LoadPlugins();
            _Logger.Log("Plugins loaded", this);
            return Response.Success();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            return Response.Failure($"Loading plugins failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Runs one connection attempt to the end and sets the final status itself, so it is right whether the attempt
    /// finishes while the user waits, after the timeout, or because it was cancelled.
    /// </summary>
    private async Task<IResponse<bool>> RunStartAsync(CancellationTokenSource cancellation)
    {
        try
        {
            await _Bot.StartAsync(cancellation.Token);
            AttachClient(_Bot.Client);
            _Logger.Log("Application started", this);
            return Response.Success();
        }
        catch (OperationCanceledException)
        {
            _Logger.Log("Connection attempt cancelled", this, LogType.Warning);
            SetStatus(BotStatus.Offline);
            return Response.Failure("The connection attempt was cancelled.");
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            SetStatus(BotStatus.Offline);
            return Response.Failure(DescribeStartFailure(ex));
        }
        finally
        {
            Interlocked.CompareExchange(ref _StartCancellation, null, cancellation);
        }
    }

    private void AttachClient(DiscordSocketClient client)
    {
        if (!ReferenceEquals(_Client, client))
        {
            DetachClient();
            _Client = client;
            client.Connected += OnConnected;
            client.Disconnected += OnDisconnected;
            client.LatencyUpdated += OnLatencyUpdated;
            client.JoinedGuild += OnGuildsChanged;
            client.LeftGuild += OnGuildsChanged;
            client.GuildAvailable += OnGuildsChanged;
            client.GuildUnavailable += OnGuildsChanged;
        }

        OnlineSince ??= DateTime.Now;
        SetStatus(client.ConnectionState == ConnectionState.Connected ? BotStatus.Online : BotStatus.Reconnecting);
    }

    private void DetachClient()
    {
        if (_Client is { } client)
        {
            client.Connected -= OnConnected;
            client.Disconnected -= OnDisconnected;
            client.LatencyUpdated -= OnLatencyUpdated;
            client.JoinedGuild -= OnGuildsChanged;
            client.LeftGuild -= OnGuildsChanged;
            client.GuildAvailable -= OnGuildsChanged;
            client.GuildUnavailable -= OnGuildsChanged;
        }

        _Client = null;
        OnlineSince = null;
        lock (_LatencyLock)
        {
            _LatencyHistory.Clear();
        }
    }

    private Task OnConnected()
    {
        if (Status == BotStatus.Reconnecting)
        {
            SetStatus(BotStatus.Online);
        }

        return Task.CompletedTask;
    }

    private Task OnDisconnected(Exception _)
    {
        if (Status == BotStatus.Online)
        {
            SetStatus(BotStatus.Reconnecting);
        }

        return Task.CompletedTask;
    }

    private Task OnLatencyUpdated(int _, int latency)
    {
        lock (_LatencyLock)
        {
            _LatencyHistory.Enqueue(latency);
            while (_LatencyHistory.Count > LatencyHistorySize)
            {
                _LatencyHistory.Dequeue();
            }
        }

        Changed?.Invoke();
        return Task.CompletedTask;
    }

    private Task OnGuildsChanged(SocketGuild _)
    {
        Changed?.Invoke();
        return Task.CompletedTask;
    }

    private void SetStatus(BotStatus status)
    {
        Status = status;
        Changed?.Invoke();
    }

    private void SetPluginsBusy(bool busy)
    {
        PluginsBusy = busy;
        Changed?.Invoke();
    }

    private static string DescribeStartFailure(Exception ex) =>
        ex.Message.Contains("401") ? "Discord rejected the token. Update it in Settings." : ex.Message;

    public void Dispose()
    {
        DetachClient();
        _Gate.Dispose();
    }
}

public sealed record GuildSummary(ulong Id, string Name, string? IconUrl, int MemberCount);
