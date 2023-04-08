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

            if (DiscordBot.DiscordBot.Instance is null)
            {
                new DiscordBot.DiscordBot(Config.Data["token"], Config.Data["prefix"]);
                await DiscordBot.DiscordBot.Instance.Start();
                await DiscordBot.DiscordBot.Instance.LoadPlugins();
            }

            BotModel model = new BotModel();
            model.StartStatus = DiscordBot.DiscordBot.Instance._boot.client.ConnectionState.ToString();
            model.BotName = DiscordBot.DiscordBot.Instance._boot.client.CurrentUser.Username;
            model.PluginsLoaded = PluginManager.Loaders.PluginLoader.PluginsLoaded;

            model.SlashCommands = PluginManager.Loaders.PluginLoader.SlashCommands;
            model.Events = PluginManager.Loaders.PluginLoader.Events;
            model.Commands = PluginManager.Loaders.PluginLoader.Commands;

            return View(model);

        }
    }
}
