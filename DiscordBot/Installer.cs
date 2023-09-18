using System;
using PluginManager;
using Spectre.Console;

namespace DiscordBot;

public static class Installer
{
    public static void GenerateStartupConfig()
    {
        AnsiConsole.WriteLine("Welcome to the [bold]SethBot[/] installer !");
        AnsiConsole.WriteLine("First, we need to configure the bot. Don't worry, it will be quick !");
        
        var token = AnsiConsole.Ask<string>("Please enter the bot token :");
        var prefix = AnsiConsole.Ask<string>("Please enter the bot prefix :");
        var serverId = AnsiConsole.Ask<string>("Please enter the Server ID :");
        
        Config.AppSettings.Add("token", token);
        Config.AppSettings.Add("prefix", prefix);
        Config.AppSettings.Add("ServerID", serverId);

        Config.AppSettings.SaveToFile();
        
        AnsiConsole.MarkupLine("[bold]Config saved ![/]");
        
        Config.Logger.Log("Config Saved", "Installer", isInternal: true);
    }
}
