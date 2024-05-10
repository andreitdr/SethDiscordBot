using System;
using PluginManager;
using System.Threading.Tasks;
using Spectre.Console;

namespace DiscordBot;

public static class Installer
{
    private static async Task AskForConfig(string key, string message)
    {
        var value = AnsiConsole.Ask<string>($"[green]{message}[/]");

        if (string.IsNullOrWhiteSpace(value))
        {
            AnsiConsole.MarkupLine($"Invalid {key} !");

            Environment.Exit(-20);
        }

        Config.AppSettings.Add(key, value);
    }
    public static async Task GenerateStartupConfig()
    {

        if(!Config.AppSettings.ContainsKey("token"))
            await AskForConfig("token", "Token:");

        if(!Config.AppSettings.ContainsKey("prefix"))
            await AskForConfig("prefix", "Prefix:");

        if(!Config.AppSettings.ContainsKey("ServerID"))
            await AskForConfig("ServerID", "Server ID:");

        await Config.AppSettings.SaveToFile();

        Config.Logger.Log("Config Saved", typeof(Installer));
    }
}
