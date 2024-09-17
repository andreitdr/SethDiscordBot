using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DiscordBotCore;
using DiscordBotCore.Others;
using Spectre.Console;

namespace DiscordBot;

public static class Installer
{
    private static string AskForConfig(string key, string message)
    {
        var value = AnsiConsole.Ask<string>($"[green]{message}[/]");

        if (!string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        AnsiConsole.MarkupLine($"Invalid {key} !");

        Environment.Exit(-20);

        return value;

    }
    
    public static async Task GenerateStartupConfig()
    {
        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token"))
        {
            var response = AskForConfig("token", "Token:");
            Application.CurrentApplication.ApplicationEnvironmentVariables.Add("token", response);
        }

        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix"))
        {
            var response = AskForConfig("prefix", "Prefix:");
            Application.CurrentApplication.ApplicationEnvironmentVariables.Add("prefix", response);
        }

        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID"))
        {
            var         response  = AskForConfig("ServerID", "Please enter the server Ids where the bot will be used (separated by ;):");
            List<ulong> serverIds = new List<ulong>();
            foreach (var id in response.Split(';'))
            {
                if(!ulong.TryParse(id, out ulong sID))
                {
                    Application.Logger.Log($"Invalid server ID {id}", LogType.Warning);
                }
                
                serverIds.Add(sID);
            }
            
            if(!serverIds.Any())
            {
                Application.Logger.Log($"No valid server id provided", LogType.Critical);
                Environment.Exit(-20);
            }
            
            Application.CurrentApplication.ApplicationEnvironmentVariables.Add("ServerID", serverIds);
        }

        await Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();

        Application.Logger.Log("Config Saved", typeof(Installer));
    }
}
