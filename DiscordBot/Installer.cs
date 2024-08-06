using System;
using DiscordBotCore;
using System.Threading.Tasks;
using Spectre.Console;

namespace DiscordBot;

public static class Installer
{
    private static string AskForConfig(string key, string message)
    {
        var value = AnsiConsole.Ask<string>($"[green]{message}[/]");

        if (string.IsNullOrWhiteSpace(value))
        {
            AnsiConsole.MarkupLine($"Invalid {key} !");

            Environment.Exit(-20);
        }

        return value;

    }
    public static async Task GenerateStartupConfig()
    {

        if(!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token"))
        {
            string response = AskForConfig("token", "Token:");
            Application.CurrentApplication.ApplicationEnvironmentVariables.Add("token", response);
        }

        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix"))
        {
            string response = AskForConfig("prefix", "Prefix:");
            Application.CurrentApplication.ApplicationEnvironmentVariables.Add("prefix", response);
        }

        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID"))
        {
            string response = AskForConfig("ServerID", "Please enter the server Ids where the bot will be used (separated by ;):");
            Application.CurrentApplication.ApplicationEnvironmentVariables.Add("ServerID", response);
        }

        await Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();

        Application.Logger.Log("Config Saved", typeof(Installer));
    }
}
