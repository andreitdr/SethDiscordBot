using Discord;
using System;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Core;
using PluginManager.Others;
using PluginManager.LanguageSystem;
using PluginManager.Online;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using PluginManager.Items;

namespace DiscordBot
{
    public class Program
    {
        private static bool loadPluginsOnStartup = false;
        private static bool listPluginsAtStartup = false;
        private static bool listLanguagAtStartup = false;
        //private static bool ShowStartupMessage   = true;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        [Obsolete]
        public static void Main(string[] args)
        {
            Directory.CreateDirectory("./Data/Resources");
            Directory.CreateDirectory("./Data/Languages");
            Directory.CreateDirectory("./Data/Plugins/Commands");
            Directory.CreateDirectory("./Data/Plugins/Events");
            Directory.CreateDirectory("./Data/runtime");

            AppDomain.CurrentDomain.AppendPrivatePath("./Data/runtime");

            if (!File.Exists("./Data/Resources/DiscordBotCore.data") || (Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN", '=').Length != 59 && Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN", '=').Length != 70))
            {
                File.WriteAllText("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN=token\nBOT_PREFIX=!\n");
                while (true)
                {
                    Console.WriteLine("Please insert your token: ");
                    Console.Write("TOKEN: ");
                    string botToken = Console.ReadLine();
                    if (botToken.Length == 59 || botToken.Length == 70)
                    {
                        string prefix                                        = Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_PREFIX", '=');
                        if (prefix == string.Empty || prefix == null) prefix = "!";
                        File.WriteAllText("./Data/Resources/DiscordBotCore.data", $"BOT_TOKEN={botToken}\nBOT_PREFIX={prefix}\n");
                        break;
                    }
                    else
                        Console.WriteLine("Invalid Token !");
                }
            }

            HandleInput(args).Wait();
        }

        /// <summary>
        /// Reset all settings for the bot
        /// </summary>
        private static Task ResetSettings()
        {
            string[] files = Directory.GetFiles(@"./Data/Resources");
            foreach (string file in files) File.Delete(file);
            return Task.CompletedTask;
        }

        /// <summary>
        /// The main loop for the discord bot
        /// </summary>
        /// <param name="discordbooter">The discord booter used to start the application</param>
        private static Task NoGUI(Boot discordbooter)
        {
            Language.LoadLanguage();

            ConsoleCommandsHandler consoleCommandsHandler = new ConsoleCommandsHandler(discordbooter.client);
            if (loadPluginsOnStartup) consoleCommandsHandler.HandleCommand("lp");
            if (listPluginsAtStartup) consoleCommandsHandler.HandleCommand("listplugs");
            if (listLanguagAtStartup) consoleCommandsHandler.HandleCommand("listlang");

            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                string cmd = Console.ReadLine();
                consoleCommandsHandler.HandleCommand(cmd);
            }
        }

        /// <summary>
        /// Start the bot without user interface
        /// </summary>
        /// <returns>Returns the boot loader for the Discord Bot</returns>
        private static async Task<Boot> StartNoGUI()
        {
            Console.Clear();
            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Discord BOT for Cross Platform");
            Console.WriteLine("Created by: Wizzy\nDiscord: Wizzy#9181");

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("============================ Discord BOT - Cross Platform ============================");
            string token  = Functions.readCodeFromFile(Functions.dataFolder + "DiscordBotCore.data", "BOT_TOKEN", '=');
            string prefix = Functions.readCodeFromFile(Functions.dataFolder + "DiscordBotCore.data", "BOT_PREFIX", '=');

            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
            return discordbooter;
        }

        /// <summary>
        /// Clear folder
        /// </summary>
        /// <param name="d">Directory path</param>
        private static Task ClearFolder(string d)
        {
            string[] files    = Directory.GetFiles(d);
            int      fileNumb = files.Length;
            for (var i = 0; i < fileNumb; i++)
            {
                File.Delete(files[i]);
                Console.WriteLine("Deleting : " + files[i]);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        /// Handle user input arguments from the startup of the application
        /// </summary>
        /// <param name="args">The arguments</param>
        private static async Task HandleInput(string[] args)
        {
            if (args.Length == 0)
            {
                if (File.Exists("./ref/startupArguments.txt"))
                {
                    var lines = await File.ReadAllLinesAsync("./ref/startupArguments.txt");
                    args = lines;
                }
            }

            int len = args.Length;
            if (len == 1 && args[0] == "--help")
            {
                Console.WriteLine("Available commands:\n--exec -> start the bot with tools enabled");
                return;
            }

            if (len == 1 && args[0] == "--logout")
            {
                File.Delete(Functions.dataFolder + "DiscordBotCore.dat");
                await Task.Run(async () =>
                {
                    await Task.Delay(1000);
                    Environment.Exit(0x08);
                });
                return;
            }

            if (len >= 2 && args[0] == "--encrypt")
            {
                string s2e = args.MergeStrings(1);
                Console.WriteLine("MD5: " + await Cryptography.CreateMD5(s2e));
                Console.WriteLine("SHA356: " + await Cryptography.CreateSHA256(s2e));
                return;
            }

            if (len > 0 && (args.Contains("--cmd") || args.Contains("--args") || args.Contains("--nomessage")))
            {
                if (args.Contains("lp") || args.Contains("loadplugins")) loadPluginsOnStartup = true;
                if (args.Contains("listplugs")) listPluginsAtStartup                          = true;
                if (args.Contains("listlang")) listLanguagAtStartup                           = true;
                //if (args.Contains("--nomessage")) ShowStartupMessage                          = false;
                len = 0;
            }


            if (len == 0 || args[0] != "--exec" && args[0] != "--execute")
            {
                Boot b = await StartNoGUI();
                await NoGUI(b);
                return;
            }

            Console.ForegroundColor = ConsoleColor.DarkYellow;
            Console.WriteLine("Execute command interface noGUI\n\n");
            Console.WriteLine(
                "\tCommand name\t\t\t\tDescription\n" +
                "-- help | -help\t\t ------ \tDisplay the help message\n" +
                "--reset-full\t\t ------ \tReset all files (clear files)\n" +
                "--reset-settings\t ------ \tReset only bot settings\n" +
                "--reset-logs\t\t ------ \tClear up the output folder\n" +
                "--start\t\t ------ \tStart the bot\n" +
                "exit\t\t\t ------ \tClose the application"
            );
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write("> ");
                string[] message = Console.ReadLine().Split(' ');

                switch (message[0])
                {
                    case "--reset-settings":
                        await ResetSettings();
                        Console.WriteLine("Successfully reseted all settings !");
                        break;
                    case "--help":
                    case "-help":
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(
                            "\tCommand name\t\t\t\tDescription\n" +
                            "-- help | -help\t\t ------ \tDisplay the help message\n" +
                            "--reset-full\t\t ------ \tReset all files (clear files)\n" +
                            "--reset-settings\t ------ \tReset only bot settings\n" +
                            "--reset-logs\t\t ------ \tClear up the output folder\n" +
                            "--start\t\t ------ \tStart the bot\n" +
                            "exit\t\t\t ------ \tClose the application"
                        );
                        break;
                    case "--reset-full":
                        await ClearFolder("./Data/Resources/");
                        await ClearFolder("./Output/Logs/");
                        await ClearFolder("./Output/Errors");
                        await ClearFolder("./Data/Languages/");
                        await ClearFolder("./Data/Plugins/Addons");
                        await ClearFolder("./Data/Plugins/Commands");
                        await ClearFolder("./Data/Plugins/Events");
                        Console.WriteLine("Successfully cleared all folders");
                        break;
                    case "--reset-logs":
                        await ClearFolder("./Output/Logs");
                        await ClearFolder("./Output/Errors");
                        Console.WriteLine("Successfully cleard logs folder");
                        break;
                    case "--exit":
                    case "exit":
                        Environment.Exit(0);
                        break;
                    case "--start":
                        Boot booter = await StartNoGUI();
                        await NoGUI(booter);
                        return;
                    default:
                        Console.WriteLine("Failed to execute command " + message[0]);
                        break;
                }
            }
        }
    }
}
