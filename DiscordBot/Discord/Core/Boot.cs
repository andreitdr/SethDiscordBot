using Discord;
using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Threading;
using System.Threading.Tasks;
using PluginManager;
using static PluginManager.Others.Functions;

namespace DiscordBot.Discord.Core
{
    internal class Boot
    {
        /// <summary>
        /// The bot prefix
        /// </summary>
        public readonly string botPrefix;

        /// <summary>
        /// The bot token
        /// </summary>
        public readonly string botToken;


        /// <summary>
        /// Checks if the bot is ready
        /// </summary>
        /// <value> true if the bot is ready, othwerwise false </value>
        public bool isReady { get; private set; } = false;

        /// <summary>
        /// The bot client
        /// </summary>
        public DiscordSocketClient client;

        /// <summary>
        /// The bot command handler
        /// </summary>
        private CommandHandler commandServiceHandler;

        /// <summary>
        /// The command service
        /// </summary>
        private CommandService service;

        /// <summary>
        /// The main Boot constructor
        /// </summary>
        /// <param name="botToken">The bot token</param>
        /// <param name="botPrefix">The bot prefix</param>
        public Boot(string botToken, string botPrefix)
        {
            this.botPrefix = botPrefix;
            this.botToken = botToken;
        }

        /// <summary>
        /// The start method for the bot. This method is used to load the bot
        /// </summary>
        /// <returns>Task</returns>
        public async Task Awake()
        {
            client = new DiscordSocketClient();
            service = new CommandService();

            CommonTasks();

            await client.LoginAsync(TokenType.Bot, botToken);
            await client.StartAsync();

            commandServiceHandler = new CommandHandler(client, service, botPrefix);
            await commandServiceHandler.InstallCommandsAsync();

            await Task.Delay(2000);
            while (!isReady) ;

        }

        private void CommonTasks()
        {
            if (client == null) return;
            client.LoggedOut += Client_LoggedOut;
            client.Log += Log;
            client.LoggedIn += LoggedIn;
            client.Ready += Ready;
        }

        private Task Client_LoggedOut()
        {
            WriteLogFile("Successfully Logged Out");
            Log(new LogMessage(LogSeverity.Info, "Boot", "Successfully logged out from discord !"));
            return Task.CompletedTask;
        }

        private Task Ready()
        {
            Console.Title = "ONLINE";
            isReady = true;

            new Thread(async () =>
                {
                    while (true)
                    {
                        Config.SaveConfig();
                        Thread.Sleep(10000);
                    }
                }
            ).Start();

            return Task.CompletedTask;
        }

        private Task LoggedIn()
        {
            Console.Title = "CONNECTED";
            WriteLogFile("The bot has been logged in at " + DateTime.Now.ToShortDateString() + " (" +
                         DateTime.Now.ToShortTimeString() + ")");
            return Task.CompletedTask;
        }

        private Task Log(LogMessage message)
        {
            switch (message.Severity)
            {
                case LogSeverity.Error:
                case LogSeverity.Critical:
                    WriteErrFile(message.Message);

                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("[ERROR] " + message.Message);
                    Console.ForegroundColor = ConsoleColor.White;

                    break;

                case LogSeverity.Info:
                case LogSeverity.Debug:
                    WriteLogFile(message.Message);

                    Console.ForegroundColor = ConsoleColor.Cyan;
                    Console.WriteLine("[INFO] " + message.Message);
                    Console.ForegroundColor = ConsoleColor.White;


                    break;
            }

            return Task.CompletedTask;
        }
    }
}
