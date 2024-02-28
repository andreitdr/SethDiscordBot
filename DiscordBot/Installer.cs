using System;
using PluginManager;
using System.Threading.Tasks;
using Spectre.Console;

namespace DiscordBot;

public static class Installer
{
    public static async Task GenerateStartupConfig()
    {
        var token     = AnsiConsole.Ask<string>("[green]Token:[/]");
        var botPrefix = AnsiConsole.Ask<string>("[yellow]Prefix:[/]");
        var serverId  = AnsiConsole.Ask<string>("[deeppink1]Server ID:[/]");

        if (string.IsNullOrWhiteSpace(serverId)) serverId = string.Empty;

        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(botPrefix))
        {
            AnsiConsole.MarkupLine("Invalid token or prefix !");

            Environment.Exit(-20);
        }

        Config.AppSettings.Add("token", token);
        Config.AppSettings.Add("prefix", botPrefix);
        Config.AppSettings.Add("ServerID", serverId);

        await Config.AppSettings.SaveToFile();

        Config.Logger.Log("Config Saved", typeof(Installer));
    }
}
