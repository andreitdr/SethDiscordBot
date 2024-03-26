using System.Threading.Tasks;

using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Others;

namespace DiscordBotUI.Bot
{
    internal class DiscordBot
    {
        private readonly string[] _StartArguments;

        public DiscordBot(string[] args)
        {
            this._StartArguments = args;
        }

        public async Task InitializeBot()
        {
            string token = Config.AppSettings["token"];
            string prefix = Config.AppSettings["prefix"];
            PluginManager.Bot.Boot discordBooter = new PluginManager.Bot.Boot(token, prefix);
            await discordBooter.Awake();
        }

        public async Task LoadPlugins()
        {
            var loader = new PluginLoader(Config.DiscordBot.Client);

            loader.OnCommandLoaded += (data) =>
            {
                if (data.IsSuccess)
                {
                    Config.Logger.Log("Successfully loaded command : " + data.PluginName, typeof(ICommandAction),
                        LogType.INFO
                    );
                }

                else
                {
                    Config.Logger.Log("Failed to load command : " + data.PluginName + " because " + data.ErrorMessage,
                        typeof(ICommandAction), LogType.ERROR
                    );
                }
            };
            loader.OnEventLoaded += (data) =>
            {
                if (data.IsSuccess)
                {
                    Config.Logger.Log("Successfully loaded event : " + data.PluginName, typeof(ICommandAction),
                        LogType.INFO
                    );
                }
                else
                {
                    Config.Logger.Log("Failed to load event : " + data.PluginName + " because " + data.ErrorMessage,
                        typeof(ICommandAction), LogType.ERROR
                    );
                }
            };

            loader.OnSlashCommandLoaded += (data) =>
            {
                if (data.IsSuccess)
                {
                    Config.Logger.Log("Successfully loaded slash command : " + data.PluginName, typeof(ICommandAction),
                        LogType.INFO
                    );
                }
                else
                {
                    Config.Logger.Log("Failed to load slash command : " + data.PluginName + " because " + data.ErrorMessage,
                        typeof(ICommandAction), LogType.ERROR
                    );
                }
            };

            await loader.LoadPlugins();
        }

    }
}
