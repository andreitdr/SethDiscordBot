using DiscordBotCore.Logging;
using DiscordBotCore.Utilities.Responses;
using IConfiguration = DiscordBotCore.Configuration.IConfiguration;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Services;

/// <summary>
/// Ties the bot to the app's lifetime: with <c>AutoStart</c> on, starts it and loads the plugins when the app starts,
/// so a restarted container comes back online by itself. When the app shuts down, stops the bot so it logs out and
/// removes its slash commands instead of just dropping the connection.
/// </summary>
public sealed class BotLifetimeService : BackgroundService
{
    public const string AutoStartConfigurationKey = "AutoStart";

    private readonly BotStateService _Bot;
    private readonly IConfiguration _Configuration;
    private readonly ILogger _Logger;

    public BotLifetimeService(BotStateService bot, IConfiguration configuration, ILogger logger)
    {
        _Bot = bot;
        _Configuration = configuration;
        _Logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_Configuration.Get(AutoStartConfigurationKey, false))
        {
            return;
        }

        if (!_Bot.HasToken)
        {
            _Logger.Log("Auto-start is on, but no bot token is set. Set one in Settings.", this, LogType.Warning);
            return;
        }

        _Logger.Log("Auto-start: starting the bot", this);
        IResponse<bool> started = await _Bot.StartAsync();
        if (!started.IsSuccess && _Bot.Status == BotStatus.Connecting)
        {
            // Slow to reach Ready: the attempt goes on in the background, so keep waiting for it.
            started = await _Bot.WaitForPendingStartAsync().WaitAsync(stoppingToken);
        }

        if (!started.IsSuccess || !_Bot.IsRunning)
        {
            _Logger.Log($"Auto-start: the bot didn't start. {started.Message}", this, LogType.Warning);
            return;
        }

        IResponse<bool> loaded = await _Bot.LoadPluginsAsync();
        if (!loaded.IsSuccess)
        {
            _Logger.Log($"Auto-start: loading plugins failed. {loaded.Message}", this, LogType.Warning);
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await base.StopAsync(cancellationToken);

        if (_Bot.IsRunning || _Bot.Status == BotStatus.Connecting)
        {
            _Logger.Log("The app is shutting down, stopping the bot", this);
            await _Bot.StopAsync().WaitAsync(cancellationToken);
        }
    }
}
