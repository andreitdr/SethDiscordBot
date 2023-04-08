using DiscordBotUI.DiscordBot;
using DiscordBotUI.Models.Bot;

using Microsoft.AspNetCore.Mvc;

using PluginManager;

namespace DiscordBotUI.Controllers
{
    public class BotController : Controller
    {
        public async Task<IActionResult> Start()
        {
            if (!Config.Data.ContainsKey("ServerID") || !Config.Data.ContainsKey("token") ||
            Config.Data["token"] == null ||
            (Config.Data["token"]?.Length != 70 && Config.Data["token"]?.Length != 59) ||
            !Config.Data.ContainsKey("prefix") || Config.Data["prefix"] == null ||
            Config.Data["prefix"]?.Length != 1)
                return RedirectToAction("Settings", "Home");

            bool isReady = true;
            if (DiscordBot.DiscordBot.Instance is null)
            {
                isReady=false;
                await Task.Run(async() => {
                    new DiscordBot.DiscordBot(Config.Data["token"], Config.Data["prefix"]);
                    await DiscordBot.DiscordBot.Instance.Start();
                    await DiscordBot.DiscordBot.Instance.LoadPlugins();
                    new PluginManager.Items.ConsoleCommandsHandler(DiscordBot.DiscordBot.Instance._boot.client);
                }).ContinueWith((t) => {
                    isReady = true;
                });
            }

            while (!isReady)
                await Task.Delay(100);

                await Task.Delay(2000);

            BotModel model = new BotModel();
            model.StartStatus = DiscordBot.DiscordBot.Instance._boot.client.ConnectionState.ToString();
            model.BotName = DiscordBot.DiscordBot.Instance._boot.client.CurrentUser.Username;
            model.PluginsLoaded = PluginManager.Loaders.PluginLoader.PluginsLoaded;

            model.SlashCommands = PluginManager.Loaders.PluginLoader.SlashCommands ?? new List<PluginManager.Interfaces.DBSlashCommand>();
            model.Events = PluginManager.Loaders.PluginLoader.Events ?? new List<PluginManager.Interfaces.DBEvent>();
            model.Commands = PluginManager.Loaders.PluginLoader.Commands ?? new List<PluginManager.Interfaces.DBCommand>();

            return View(model);

        }

        public async Task<IActionResult> PluginsPage()
        {
            if(DiscordBot.DiscordBot.Instance is null) {
                return RedirectToAction("Start");
            }

            PluginsPageModel model = new PluginsPageModel();
            if (PluginManager.Loaders.PluginLoader.Commands != null)
                model.InstalledCommands = PluginManager.Loaders.PluginLoader.Commands.Select(x => x.Command).ToList();
            else model.InstalledCommands = new List<string>();
            if (PluginManager.Loaders.PluginLoader.Events != null)
                model.InstalledEvents = PluginManager.Loaders.PluginLoader.Events.Select(x => x.Name).ToList();
            else model.InstalledEvents = new List<string>();
            if (PluginManager.Loaders.PluginLoader.SlashCommands != null)
                model.InstalledSlashCommands = PluginManager.Loaders.PluginLoader.SlashCommands.Select(x => x.Name).ToList();
            else model.InstalledSlashCommands = new List<string>();
            model.PluginsManager = new("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/Plugins.txt");
            List<string[]> plugins = await model.PluginsManager.ListAvailablePlugins();
            if (plugins == null) return RedirectToAction("Start");

            model.Plugins = plugins;

            return View(model);
        }

        [HttpGet]
        public async Task<int> InstallPlugin(string pluginName)
        {
            try
            {
                if(!DiscordBot.DiscordBot.Instance._boot.isReady)
                    return 1;
                await PluginManager.Items.ConsoleCommandsHandler.ExecuteCommad("dwplug " + pluginName);
                return 0;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex);
                Console.WriteLine(ex.ToString());
                return 1;
            }

        }
    }
}
