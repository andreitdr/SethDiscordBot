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

        if (!Config.Data.ContainsKey("token"))
        {
            Console.WriteLine("Please enter the bot token :");
            var token = Console.ReadLine();
            Config.Data.Add("token", token);
        }

        if (!Config.Data.ContainsKey("prefix"))
        {
            Console.WriteLine("Please enter the bot prefix :");
            var prefix = Console.ReadLine();
            Config.Data.Add("prefix", prefix);
        }

        if (!Config.Data.ContainsKey("ServerID"))
        {
            Console.WriteLine("Please enter the Server ID :");
            var serverId = Console.ReadLine();
            Config.Data.Add("ServerID", serverId);
        }

        Config.Logger.Log("Config Saved", "Installer");

        Config.Data.Save();

        Console.WriteLine("Config saved !");
    }

    public static async Task SetupPluginDatabase()
    {
        Console.WriteLine("The plugin database is required to run the bot but there is nothing configured yet.");
        Console.WriteLine("Please select one option : ");
        Console.WriteLine("1. Download the official database file");
        Console.WriteLine("2. Create a new (CUSTOM) database file");
        var choice = 0;
        Console.Write("Choice : ");
        choice = int.Parse(Console.ReadLine());
        if (choice != 1 && choice != 2)
        {
            Console.WriteLine("Invalid choice !");
            Console.WriteLine("Please restart the installer !");
            Console.ReadKey();
            Environment.Exit(0);
        }

        if (choice == 1)
            await DownloadPluginDatabase();

        if (choice == 2)
        {
            Console.WriteLine("Do you have a url to a valid database file ? (y/n)");
            var answer = Console.ReadLine();
            if (answer == "y")
            {
                Console.WriteLine("Please enter the url :");
                var url = Console.ReadLine();
                await DownloadPluginDatabase(url);
                return;
            }

            Console.WriteLine("Do you want to create a new database file ? (y/n)");
            answer = Console.ReadLine();
            if (answer == "y")
            {
                Console.WriteLine("A new file will be generated at ./Data/Resources/URLs.json");
                Console.WriteLine("Please edit the file and restart the bot !");
                Directory.CreateDirectory("./Data/Resources");
                await File.WriteAllTextAsync("./Data/Resources/URLs.json",
                                             @"
                    {
                        ""PluginList"": """",
                        ""PluginVersions"": """",
                        ""StartupMessage"": """",
                        ""SetupKeys"": """",
                        ""Versions"": """",
                        ""Changelog"": """",
                        ""LinuxBot"": """",
                        ""WindowsLauncher"": """",
                    }
                    ".Replace("                    ", ""));
                Environment.Exit(0);
            }
        }
    }

    private static async Task DownloadPluginDatabase(
        string url = "https://raw.githubusercontent.com/andreitdr/SethDiscordBot/gh-pages/defaultURLs.json")
    {
        var path = "./Data/Resources/URLs.json";

        Directory.CreateDirectory("./Data/Resources");
        var spinner = new Utilities.Utilities.Spinner();
        spinner.Start();
        await ServerCom.DownloadFileAsync(url, path, null);
        spinner.Stop();
    }
}
