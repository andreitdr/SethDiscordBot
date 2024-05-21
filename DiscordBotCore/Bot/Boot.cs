using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore.Others;


namespace DiscordBotCore.Bot;

public class Boot
{
    /// <summary>
    ///     The bot prefix
    /// </summary>
    public readonly string BotPrefix;

    /// <summary>
    ///     The bot token
    /// </summary>
    public readonly string BotToken;

    /// <summary>
    ///     The bot client
    /// </summary>
    public DiscordSocketClient Client;

    /// <summary>
    ///     The bot command handler
    /// </summary>
    private CommandHandler _commandServiceHandler;

    /// <summary>
    ///     The command service
    /// </summary>
    private CommandService _service;

    /// <summary>
    ///     The main Boot constructor
    /// </summary>
    /// <param name="botToken">The bot token</param>
    /// <param name="botPrefix">The bot prefix</param>
    public Boot(string botToken, string botPrefix)
    {
        this.BotPrefix = botPrefix;
        this.BotToken  = botToken;
    }


    /// <summary>
    ///     Checks if the bot is ready
    /// </summary>
    /// <value> true if the bot is ready, otherwise false </value>
    public bool IsReady { get; private set; }

    /// <summary>
    ///     The start method for the bot. This method is used to load the bot
    /// </summary>
    /// <param name="config">
    ///     The discord socket config. If null then the default one will be applied (AlwaysDownloadUsers=true,
    ///     UseInteractionSnowflakeDate=false, GatewayIntents=GatewayIntents.All)
    /// </param>
    /// <returns>Task</returns>
    public async Task Awake(DiscordSocketConfig? config = null)
    {
        if (config is null)
            config = new DiscordSocketConfig
            {
                AlwaysDownloadUsers = true,

                //Disable system clock checkup (for responses at slash commands)
                UseInteractionSnowflakeDate = false,
                GatewayIntents              = GatewayIntents.All
            };

        Client  = new DiscordSocketClient(config);
        _service = new CommandService();

        CommonTasks();

        await Client.LoginAsync(TokenType.Bot, BotToken);

        await Client.StartAsync();

        _commandServiceHandler = new CommandHandler(Client, _service, BotPrefix);

        await _commandServiceHandler.InstallCommandsAsync();

        Application.CurrentApplication.DiscordBotClient = this;

        while (!IsReady) ;
    }

    
    private void CommonTasks()
    {
        if (Client == null) return;
        Client.Log          += Log;
        Client.LoggedIn     += LoggedIn;
        Client.Ready        += Ready;
        Client.Disconnected += Client_Disconnected;
    }

    private async Task Client_Disconnected(Exception arg)
    {
        if (arg.Message.Contains("401"))
        {
            Application.CurrentApplication.ApplicationEnvironmentVariables.Remove("token");
            Application.CurrentApplication.Logger.Log("The token is invalid.", this, LogType.CRITICAL);
            await Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();
            await Task.Delay(3000);
            
            Process.Start(Environment.ProcessPath);
            Environment.Exit(0);
        }
    }

    private Task Ready()
    {
        IsReady = true;
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
                Application.CurrentApplication.Logger.Log(message.Message, this, LogType.ERROR);
                break;

            case LogSeverity.Info:
            case LogSeverity.Debug:
                Application.CurrentApplication.Logger.Log(message.Message, this, LogType.INFO);


                break;
        }

        return Task.CompletedTask;
    }
}
