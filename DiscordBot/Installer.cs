using System.Diagnostics;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PluginManager;
using PluginManager.Others;
using PluginManager.Online;

namespace DiscordBot
{
    public static class Installer
    {

        public static void GenerateStartupConfig()
        {
            Console.WriteLine("Welcome to the SethBot installer !");
            Console.WriteLine("First, we need to configure the bot. Don't worry, it will be quick !");
            Console.WriteLine("The following information will be stored in the config.json file in the ./Data/Resources folder. You can change it later from there.");
            Console.WriteLine("The bot tokn is required to run the bot. You can get it from the Discord Developer Portal. (https://discord.com/developers/applications)");
            Console.WriteLine("Please enter the bot token :");
            var token = Console.ReadLine();

            Console.WriteLine("Please enter the bot prefix :");
            var prefix = Console.ReadLine();

            Console.WriteLine("Please enter the Server ID :");
            var serverId = Console.ReadLine();

            Config.Data.Add("token", token);
            Config.Data.Add("prefix", prefix);
            Config.Data.Add("ServerID", serverId);

            Config.Logger.Log("Config Saved", "Installer", TextType.NORMAL);

            Config.Data.Save();

            Console.WriteLine("Config saved !");

            
        }

        public static async Task SetupPluginDatabase()
        {
            Console.WriteLine("The plugin database is required to run the bot but there is nothing configured yet.");
            Console.WriteLine("Please select one option : ");
            Console.WriteLine("1. Download the official database file");
            Console.WriteLine("2. Create a new (CUSTOM) database file");
            int choice = 0;
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
                    System.Console.WriteLine("Please edit the file and restart the bot !");
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
                    return;
                }
            }
        }

        private static async Task DownloadPluginDatabase(string url = "https://raw.githubusercontent.com/Wizzy69/SethDiscordBot/gh-pages/defaultURLs.json")
        {
            string path = "./Data/Resources/URLs.json";

            Directory.CreateDirectory("./Data/Resources");
            Utilities.Utilities.ProgressBar bar = new Utilities.Utilities.ProgressBar(Utilities.ProgressBarType.NORMAL){
                Max = 100,
                Color = ConsoleColor.Green,
                NoColor = true
            };
            IProgress<float> downloadProgress = new Progress<float>(p => bar.Update(p));
            await ServerCom.DownloadFileAsync(url, path, downloadProgress, null);
            bar.Update(bar.Max);
        }
    }
}