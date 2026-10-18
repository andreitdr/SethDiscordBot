using System;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Loading;


namespace DiscordBotCore.Bot;

public class DiscordBotApplication : IDiscordBotApplication
{
    internal static IPluginLoader _InternalPluginLoader;
    
    private CommandHandler _CommandServiceHandler;
    private CommandService _Service;
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;
    private readonly IPluginLoader _PluginLoader;
    private bool _IsStopping;
    private TaskCompletionSource? _ReadySignal;
    
    public bool IsReady { get; private set; }
    
    public DiscordSocketClient Client { get; private set; }

    /// <summary>
    ///     The main Boot constructor
    /// </summary>
    public DiscordBotApplication(ILogger logger, IConfiguration configuration, IPluginLoader pluginLoader)
    {
        this._Logger = logger;
        this._Configuration = configuration;
        this._PluginLoader = pluginLoader;
        
        _InternalPluginLoader = pluginLoader;
    }

    public async Task StopAsync()
    {
        if (!IsReady)
        {
            _Logger.Log("Can not stop the bot. It is not yet initialized.", this, LogType.Warning);
            return;
        }

        if (_PluginLoader.IsLoaded)
        {
            await _PluginLoader.UnloadAllPlugins();
        }

        await TearDownClientAsync(Client);
    }

    /// <summary>
    ///     Logs in and connects to the gateway. Completes once the client is Ready. If the login fails or
    ///     <paramref name="cancellationToken"/> is cancelled first, the client is torn down and the exception is rethrown.
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        var config = new DiscordSocketConfig
        {
            AlwaysDownloadUsers = true,

            //Disable system clock checkup (for responses at slash commands)
            UseInteractionSnowflakeDate = false,
            GatewayIntents              = GatewayIntents.All
        };

        DiscordSocketClient client  = new DiscordSocketClient(config);
        
        _Service = new CommandService();
        _ReadySignal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        
        client.Log          += Log;
        client.LoggedIn     += LoggedIn;
        client.Ready        += Ready;
        client.Disconnected += Client_Disconnected;

        Client = client;
        try
        {
            await client.LoginAsync(TokenType.Bot, _Configuration.Get<string>("token"));
            await client.StartAsync();

            _CommandServiceHandler = new CommandHandler(_Logger, _PluginLoader, _Configuration, _Service);
            await _CommandServiceHandler.InstallCommandsAsync(client);

            // Ready only comes once the gateway accepts the session. Missing privileged intents or a network problem
            // mean it never does, so the caller decides how long to wait.
            await _ReadySignal.Task.WaitAsync(cancellationToken);
        }
        catch
        {
            await TearDownClientAsync(client);
            throw;
        }
    }

    private async Task TearDownClientAsync(DiscordSocketClient client)
    {
        _IsStopping = true;
        try
        {
            await client.LogoutAsync();
            await client.StopAsync();
        }
        catch (Exception ex)
        {
            _Logger.Log($"Disconnecting the client failed: {ex.Message}", this, LogType.Warning);
        }
        finally
        {
            client.Log          -= Log;
            client.LoggedIn     -= LoggedIn;
            client.Ready        -= Ready;
            client.Disconnected -= Client_Disconnected;

            await client.DisposeAsync();
            _PluginLoader.ClearDiscordClient();

            IsReady = false;
            _IsStopping = false;
        }
    }

    private async Task Client_Disconnected(Exception? arg)
    {
        if (arg is not null && arg.Message.Contains("401"))
        {
            _Configuration.Set("token", string.Empty);
            _Logger.Log("The token is invalid.", this, LogType.Critical);
            await _Configuration.SaveToFile();
        }
    }

    private Task Ready()
    {
        IsReady = true;
        _ReadySignal?.TrySetResult();
        return Task.CompletedTask;
    }

    private Task LoggedIn()
    {
        _Logger.Log("Successfully Logged In", this);
        _PluginLoader.SetDiscordClient(Client);
        return Task.CompletedTask;
    }

    private Task Log(LogMessage message)
    {
        if (message.Severity is LogSeverity.Verbose or LogSeverity.Debug)
        {
            return Task.CompletedTask;
        }

        string text = string.IsNullOrWhiteSpace(message.Message)
            ? $"{message.Source}: {message.Exception?.Message ?? "no details"}"
            : message.Message;

        // Closing the gateway on purpose makes Discord.Net report the cancelled socket; that is not an error.
        if (_IsStopping && IsExpectedShutdownException(message.Exception))
        {
            _Logger.Log(text, this, LogType.Info);
            return Task.CompletedTask;
        }

        switch (message.Severity)
        {
            case LogSeverity.Error:
            case LogSeverity.Critical:
                if (message.Exception is not null && !string.IsNullOrWhiteSpace(message.Message))
                    _Logger.Log($"{text}: {message.Exception.Message}", this, LogType.Error);
                else
                    _Logger.Log(text, this, LogType.Error);
                break;

            case LogSeverity.Warning:
                if (message.Exception is not null && !string.IsNullOrWhiteSpace(message.Message))
                    _Logger.Log($"{text}: {message.Exception.Message}", this, LogType.Warning);
                else
                    _Logger.Log(text, this, LogType.Warning);
                break;

            case LogSeverity.Info:
                _Logger.Log(text, this, LogType.Info);
                break;
        }

        return Task.CompletedTask;
    }

    private static bool IsExpectedShutdownException(Exception? exception) =>
        exception is null or OperationCanceledException or GatewayReconnectException
            or Discord.Net.WebSocketClosedException or System.Net.WebSockets.WebSocketException;
}
