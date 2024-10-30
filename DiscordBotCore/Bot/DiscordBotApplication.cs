using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore.Others;


namespace DiscordBotCore.Bot;

public class DiscordBotApplication
{
    /// <summary>
    ///     The bot prefix
    /// </summary>
    private readonly string _BotPrefix;

    /// <summary>
    ///     The bot token
    /// </summary>
    private readonly string _BotToken;

    /// <summary>
    ///     The bot client
    /// </summary>
    public DiscordSocketClient Client;

    /// <summary>
    ///     The bot command handler
    /// </summary>
    private CommandHandler _CommandServiceHandler;

    /// <summary>
    ///     The command service
    /// </summary>
    private CommandService _Service;
    
    /// <summary>
    ///     Checks if the bot is ready
    /// </summary>
    /// <value> true if the bot is ready, otherwise false </value>
    private bool IsReady { get; set; }

    /// <summary>
    ///     The main Boot constructor
    /// </summary>
    /// <param name="botToken">The bot token</param>
    /// <param name="botPrefix">The bot prefix</param>
    public DiscordBotApplication(string botToken, string botPrefix)
    {
        this._BotPrefix = botPrefix;
        this._BotToken  = botToken;
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

        Client  = new DiscordSocketClient(config);
        _Service = new CommandService();
        
        Client.Log          += Log;
        Client.LoggedIn     += LoggedIn;
        Client.Ready        += Ready;
        Client.Disconnected += Client_Disconnected;

        await Client.LoginAsync(TokenType.Bot, _BotToken);

        await Client.StartAsync();

        _CommandServiceHandler = new CommandHandler(Client, _Service, _BotPrefix);

        await _CommandServiceHandler.InstallCommandsAsync();

        Application.CurrentApplication.DiscordBotClient = this;
        
        // wait for the bot to be ready
        while (!IsReady)
        {
            await Task.Delay(100);
        }
    }

    private async Task Client_Disconnected(Exception arg)
    {
        if (arg.Message.Contains("401"))
        {
            Application.CurrentApplication.ApplicationEnvironmentVariables.Remove("token");
            Application.CurrentApplication.Logger.Log("The token is invalid.", this, LogType.Critical);
            await Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();
        }
    }

    private Task Ready()
    {
        IsReady = true;

        if (Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("CustomStatus"))
        {
            var status = Application.CurrentApplication.ApplicationEnvironmentVariables.GetDictionary<string, string>("CustomStatus");
            string type = status["Type"];
            string message = status["Message"];
            ActivityType activityType = type switch
            {
                "Playing" => ActivityType.Playing,
                "Listening" => ActivityType.Listening,
                "Watching" => ActivityType.Watching,
                "Streaming" => ActivityType.Streaming,
                _ => ActivityType.Playing
            };
            Client.SetGameAsync(message, null, activityType);
        }
        
        return Task.CompletedTask;
    }

    private Task LoggedIn()
    {
        Application.CurrentApplication.Logger.Log("Successfully Logged In", this);
        return Task.CompletedTask;
    }

    private Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Error:
            case LogSeverity.Critical:
                Application.CurrentApplication.Logger.Log(message.Message, this, LogType.Error);
                break;

            case LogSeverity.Info:
            case LogSeverity.Debug:
                Application.CurrentApplication.Logger.Log(message.Message, this, LogType.Info);


                break;
        }

        return Task.CompletedTask;
    }
}
