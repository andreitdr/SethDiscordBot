using System;
using PluginManager;
using Spectre.Console;

using System.Threading.Tasks;

namespace DiscordBot;

public static class Installer
{
    public static async Task GenerateStartupConfig()
    {
        string token     = await PluginManager.UX.UxHandler.ShowInputBox("SethBot", "Please enter the bot token:");
        string botPrefix = await PluginManager.UX.UxHandler.ShowInputBox("SethBot", "Please enter the bot prefix:");
        string serverId  = await PluginManager.UX.UxHandler.ShowInputBox("SethBot", "Please enter the Server ID:");
            
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
        
        Config.Logger.Log("Config Saved", source: typeof(Installer));
    }
}
