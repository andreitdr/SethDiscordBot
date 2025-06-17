using System;
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
            _Logger.Log("Can not stop the bot. It is not yet initialized.", this, LogType.Error);
            return;
        }
        
        await _PluginLoader.UnloadAllPlugins();
        
        await Client.LogoutAsync();
        await Client.StopAsync();
            
        Client.Log          -= Log;
        Client.LoggedIn     -= LoggedIn;
        Client.Ready        -= Ready;
        Client.Disconnected -= Client_Disconnected;
            
        await Client.DisposeAsync();
        
        
        IsReady = false;
        
    }

    /// <summary>
    ///     The start method for the bot. This method is used to load the bot
    /// </summary>
    public async Task StartAsync()
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
        
        client.Log          += Log;
        client.LoggedIn     += LoggedIn;
        client.Ready        += Ready;
        client.Disconnected += Client_Disconnected;

        Client = client;
        await client.LoginAsync(TokenType.Bot, _Configuration.Get<string>("token"));
        await client.StartAsync();

        _CommandServiceHandler = new CommandHandler(_Logger, _PluginLoader, _Configuration, _Service);

        await _CommandServiceHandler.InstallCommandsAsync(client);
        
        while (!IsReady)
        {
            await Task.Delay(100);
        }
    }

    private async Task Client_Disconnected(Exception arg)
    {
        if (arg.Message.Contains("401"))
        {
            _Configuration.Set("token", string.Empty);
            _Logger.Log("The token is invalid.", this, LogType.Critical);
            await _Configuration.SaveToFile();
        }
    }

    private Task Ready()
    {
        IsReady = true;
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
        switch (message.Severity)
        {
            case LogSeverity.Error:
            case LogSeverity.Critical:
                _Logger.Log(message.Message, this, LogType.Error);
                break;

            case LogSeverity.Info:
            case LogSeverity.Debug:
                _Logger.Log(message.Message, this, LogType.Info);


                break;
        }

        return Task.CompletedTask;
    }
}
