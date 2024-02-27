using System;
using PluginManager;
using Spectre.Console;
using System.Threading.Tasks;

namespace DiscordBot;

public static class Installer
{
    public static async Task GenerateStartupConfig()
    {
        var token     = await PluginManager.UX.UxHandler.ShowInputBox("SethBot", "Please enter the bot token:");
        var botPrefix = await PluginManager.UX.UxHandler.ShowInputBox("SethBot", "Please enter the bot prefix:");
        var serverId  = await PluginManager.UX.UxHandler.ShowInputBox("SethBot", "Please enter the Server ID:");

        if (string.IsNullOrWhiteSpace(serverId)) serverId = "NULL";

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(botPrefix))
        {
            await PluginManager.UX.UxHandler.ShowMessageBox("SethBot", "Invalid token or prefix !", PluginManager.UX.MessageBoxType.Error);
            Environment.Exit(-20);
        }

        Config.AppSettings.Add("token", token);
        Config.AppSettings.Add("prefix", botPrefix);
        Config.AppSettings.Add("ServerID", serverId);

        await Config.AppSettings.SaveToFile();

        Config.Logger.Log("Config Saved", typeof(Installer));
    }
}
