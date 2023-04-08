using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using DiscordBotUI.Models;

using DiscordBotUI.Models.Home;
using Discord.WebSocket;
using PluginManager;

namespace DiscordBotUI.Controllers;

public class HomeController : Controller
{

    public IActionResult Index()
    {
        if (!Config.Data.ContainsKey("ServerID") || !Config.Data.ContainsKey("token") ||
                Config.Data["token"] == null ||
                (Config.Data["token"]?.Length != 70 && Config.Data["token"]?.Length != 59) ||
                !Config.Data.ContainsKey("prefix") || Config.Data["prefix"] == null ||
                Config.Data["prefix"]?.Length != 1)
            return RedirectToAction("Settings");


        IndexModel model = new IndexModel();
        model.ServerID = Config.Data["ServerID"];
        model.BotPrefix = Config.Data["prefix"];

        return View(model);
    }

    public IActionResult Settings()
    {
        SettingsModel model = new SettingsModel();
        model.BotToken = Config.Data["token"];
        model.BotPrefix = Config.Data["prefix"];
        model.ServerID = Config.Data["ServerID"];
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Settings(SettingsModel model)
    {
        if (ModelState.IsValid)
        {
            Config.Data["token"] = model.BotToken;
            Config.Data["prefix"] = model.BotPrefix;
            if (model.ServerID is not null)
                Config.Data["ServerID"] = model.ServerID;
            else Config.Data["ServerID"] = null;

            Config.Data.Save();

            return RedirectToAction("Index");
        }

        return View(model);
    }

    public IActionResult Privacy()
    {
        return View();
    }
}
