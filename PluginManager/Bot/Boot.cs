using System;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Others;
using PluginManager.UX;

namespace PluginManager.Bot;

public class Boot
{
    /// <summary>
    ///     The bot prefix
    /// </summary>
    public readonly string botPrefix;

    /// <summary>
    ///     The bot token
    /// </summary>
    public readonly string botToken;

    /// <summary>
    ///     The bot client
    /// </summary>
    public DiscordSocketClient client;

    /// <summary>
    ///     The bot command handler
    /// </summary>
    private CommandHandler commandServiceHandler;

    /// <summary>
    ///     The command service
    /// </summary>
    private CommandService service;

    /// <summary>
    ///     The main Boot constructor
    /// </summary>
    /// <param name="botToken">The bot token</param>
    /// <param name="botPrefix">The bot prefix</param>
    public Boot(string botToken, string botPrefix)
    {
        this.botPrefix = botPrefix;
        this.botToken  = botToken;
    }


    /// <summary>
    ///     Checks if the bot is ready
    /// </summary>
    /// <value> true if the bot is ready, otherwise false </value>
    public bool isReady { get; private set; }

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

        client  = new DiscordSocketClient(config);
        service = new CommandService();

        CommonTasks();

        await client.LoginAsync(TokenType.Bot, botToken);

        await client.StartAsync();

        commandServiceHandler = new CommandHandler(client, service, botPrefix);

        await commandServiceHandler.InstallCommandsAsync();

        Config.DiscordBotClient = this;

        while (!isReady) ;
    }

    private void CommonTasks()
    {
        if (client == null) return;
        client.LoggedOut    += Client_LoggedOut;
        client.Log          += Log;
        client.LoggedIn     += LoggedIn;
        client.Ready        += Ready;
        client.Disconnected += Client_Disconnected;
    }

    private async Task Client_Disconnected(Exception arg)
    {
        if (arg.Message.Contains("401"))
        {
            Config.AppSettings.Remove("token");
            Config.Logger.Log("The token is invalid. Please restart the bot and enter a valid token.", typeof(Boot), LogType.CRITICAL);
            await Config.AppSettings.SaveToFile();
            await Task.Delay(4000);
            Environment.Exit(0);
        }
    }

    private async Task Client_LoggedOut()
    {
        Config.Logger.Log("Successfully Logged Out", typeof(Boot));
        await Log(new LogMessage(LogSeverity.Info, "Boot", "Successfully logged out from discord !"));
    }

    private Task Ready()
    {
        isReady = true;
        UxHandler.ShowNotification("SethBot", "Seth Discord Bot is now up and running !").Wait();
        return Task.CompletedTask;
    }

    private Task LoggedIn()
    {
        Config.Logger.Log("Successfully Logged In", typeof(Boot));
        return Task.CompletedTask;
    }

    private Task Log(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Error:
            case LogSeverity.Critical:
                Config.Logger.Log(message.Message, typeof(Boot), LogType.ERROR);
                break;

            case LogSeverity.Info:
            case LogSeverity.Debug:
                Config.Logger.Log(message.Message, typeof(Boot), LogType.INFO);


                break;
        }

        return Task.CompletedTask;
    }
}
