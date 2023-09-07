using System;
using System.IO;
using System.Threading.Tasks;
using PluginManager;
using PluginManager.Online;

namespace DiscordBot;

public static class Installer
{
    public static void GenerateStartupConfig()
    {
        Console.WriteLine("Welcome to the SethBot installer !");
        Console.WriteLine("First, we need to configure the bot. Don't worry, it will be quick !");
        Console.WriteLine("The following information will be stored in the config.json file in the ./Data/Resources folder. You can change it later from there.");
        Console.WriteLine("The bot token is required to run the bot. You can get it from the Discord Developer Portal. (https://discord.com/developers/applications)");

        if (!Config.AppSettings.ContainsKey("token"))
        {
            Console.WriteLine("Please enter the bot token :");
            var token = Console.ReadLine();
            Config.AppSettings.Add("token", token);
        }

        if (!Config.AppSettings.ContainsKey("prefix"))
        {
            Console.WriteLine("Please enter the bot prefix :");
            var prefix = Console.ReadLine();
            Config.AppSettings.Add("prefix", prefix);
        }

        if (!Config.AppSettings.ContainsKey("ServerID"))
        {
            Console.WriteLine("Please enter the Server ID :");
            var serverId = Console.ReadLine();
            Config.AppSettings.Add("ServerID", serverId);
        }

        Config.Logger.Log("Config Saved", "Installer", isInternal: true);

        Config.AppSettings.SaveToFile();

        Console.WriteLine("Config saved !");
    }
}
