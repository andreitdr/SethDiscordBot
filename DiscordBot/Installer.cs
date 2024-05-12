using System;
using DiscordBotCore;
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

        Application.CurrentApplication.ApplicationEnvironmentVariables.Add(key, value);
    }
    public static async Task GenerateStartupConfig()
    {

        if(!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token"))
            await AskForConfig("token", "Token:");

        if(!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix"))
            await AskForConfig("prefix", "Prefix:");

        if(!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID"))
            await AskForConfig("ServerID", "Server ID:");

        await Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();

        Application.CurrentApplication.Logger.Log("Config Saved", typeof(Installer));
    }
}
