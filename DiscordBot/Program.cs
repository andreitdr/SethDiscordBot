using Discord;

using System;
using System.IO;
using System.Threading.Tasks;

using PluginManager.Core;
using PluginManager.Others;
using PluginManager.Loaders;
using PluginManager.LanguageSystem;
using PluginManager.Online;

using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace DiscordBot
{
    public class Program
    {
        private static PluginsManager manager = new PluginsManager("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Plugins");
        private static LanguageManager languageManager = new LanguageManager("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Languages");

        private static bool loadPluginsOnStartup = false;
        private static bool listPluginsAtStartup = false;
        private static bool listLanguagAtStartup = false;

        private static bool PluginsLoaded = false;

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
                        string prefix = Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_PREFIX", '=');
                        if (prefix == string.Empty || prefix == null)
                            prefix = "!";
                        File.WriteAllText("./Data/Resources/DiscordBotCore.data", $"BOT_TOKEN={botToken}\nBOT_PREFIX={prefix}\n");
                        break;
                    }
                    else Console.WriteLine("Invalid Token !");
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
        private static async Task NoGUI(Boot discordbooter)
        {
            LoadLanguage();
            if (loadPluginsOnStartup)
                LoadPlugins(discordbooter);
            if (listPluginsAtStartup)
                await manager.ListAvailablePlugins();
            if (listLanguagAtStartup)
                await languageManager.ListAllLanguages();


            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console_Utilities.WriteColorText("&mConsole ", false);
                string[] data = Console.ReadLine().Split(' ');

                if (data[0].Length < 2)
                    continue; // The input command is less then 2 characters long

                switch (data[0])
                {
                    case "shutdown":
                    case "sd":
                        if (discordbooter.client.ConnectionState == ConnectionState.Connected)
                            await discordbooter.ShutDown().ContinueWith(t => { Environment.Exit(0); });
                        break;
                    case "reload":
                    case "rl":
                        if (Environment.OSVersion.Platform != PlatformID.Win32NT)
                        {
                            Console.WriteLine("This command is for windows users ONLY");
                            break;
                        }
                        Process.Start("./DiscordBot.exe", "--cmd lp");
                        if (discordbooter.client.ConnectionState == ConnectionState.Connected)
                            await discordbooter.ShutDown();
                        else Environment.Exit(0);
                        break;
                    case "listplugs":
                        await manager.ListAvailablePlugins();
                        break;

                    case "dwplug":
                        string name = data.MergeStrings(1);
                        // info[0] = plugin type
                        // info[1] = plugin link
                        // info[2] = if others are required, or string.Empty if none
                        string[] info = await manager.GetPluginLinkByName(name);
                        if (info[1] == null) // link is null
                        {
                            if (name == "")
                            {
                                Console_Utilities.WriteColorText($"Name is invalid");
                                break;
                            }
                            Console_Utilities.WriteColorText($"Failed to find plugin &b{name} &c! Use &glistplugs &ccommand to display all available plugins !");
                            break;

                        }
                        string path;
                        if (info[0] == "Command" || info[0] == "Event")
                            path = "./Data/Plugins/" + info[0] + "s/" + name + ".dll";
                        else path = $"./{info[1].Split('/')[info[1].Split('/').Length - 1]}";
                        await ServerCom.DownloadFileAsync(info[1], path);
                        Console.WriteLine("\n");

                        // check requirements if any

                        if (info.Length == 3 && info[2] != string.Empty && info[2] != null)
                        {
                            Console.WriteLine($"Downloading requirements for plugin : {name}");

                            List<string> lines = await ServerCom.ReadTextFromFile(info[2]);

                            foreach (var line in lines)
                            {
                                string[] split = line.Split(',');
                                Console.WriteLine($"\nDownloading item: {split[1]}");
                                await ServerCom.DownloadFileAsync(split[0], "./" + split[1]);
                                Console.WriteLine();

                                if (split[0].EndsWith(".zip"))
                                {

                                    Console.WriteLine($"Extracting {split[1]}");
                                    double proc = 0d;
                                    bool isExtracting = true;
                                    Console_Utilities.ProgressBar bar = new Console_Utilities.ProgressBar(100, "");

                                    IProgress<float> extractProgress = new Progress<float>(value =>
                                    {
                                        proc = value;
                                    });
                                    new Thread(new Task(() =>
                                    {
                                        while (isExtracting)
                                        {
                                            bar.Update((int)proc);
                                            if (proc >= 99.9f)
                                                break;
                                            Thread.Sleep(500);
                                        }
                                    }).Start).Start();
                                    await Functions.ExtractArchive("./" + split[1], "./", extractProgress);
                                    bar.Update(100);
                                    isExtracting = false;
                                    await Task.Delay(1000);
                                    bar.Update(100);
                                    Console.WriteLine("\n");
                                    File.Delete("./" + split[1]);



                                }

                            }
                            Console.WriteLine();
                            break;
                        }

                        break;
                    case "setlang":
                        if (data.Length == 2)
                            SetLanguage(data[1]);
                        else Console.WriteLine("Invalid arguments");
                        break;
                    case "set-setting":
                        if (data.Length >= 3)
                            Functions.WriteToSettingsFast(data[1], Functions.MergeStrings(data, 2));
                        else Console.WriteLine("Failed to write to settings. Invalid params");
                        break;
                    case "listlang":
                        await languageManager.ListAllLanguages();
                        break;
                    case "dwlang":
                        string Lname = data.MergeStrings(1);
                        string[] link = await languageManager.GetDownloadLink(Lname);
                        try
                        {
                            if (link[0] is null || link is null)
                            {
                                if (Lname == "")
                                {
                                    Console_Utilities.WriteColorText($"Name is invalid");
                                    break;
                                }
                                Console_Utilities.WriteColorText("Failed to find language &b" + Lname + " &c! Use &glistlang &ccommand to display all available languages !");
                                break;
                            }
                            if (link[1].Contains("CrossPlatform") || link[1].Contains("cp"))
                            {

                                string path2 = Functions.langFolder + Lname + ".lng";

                                await ServerCom.DownloadFileAsync(link[0], path2);
                                Console.WriteLine("\n");
                            }
                            else Console_Utilities.WriteColorText("The language you are trying to download (&b" + Lname + "&c) is not compatible with the version of this bot. User &glistlang &ccommand in order to see all available languages for your current version !\n" + link[1]);
                            break;
                        }
                        catch
                        {
                            if (Lname == "")
                            {
                                Console_Utilities.WriteColorText($"Name is invalid");
                                break;
                            }
                            Console_Utilities.WriteColorText("Failed to find language &b" + Lname + " &c! Use &glistlang &ccommand to display all available languages !");
                            break;
                        }

                    case "loadplugins":
                    case "lp":
                        if (PluginsLoaded)
                        {
                            Console_Utilities.WriteColorText("&rPlugins are already loaded");
                            break;
                        }
                        LoadPlugins(discordbooter);

                        break;
                    case "help":
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine
                        (
                            "lp | loadplugins -> load all plugins\n" +
                            "sd | shutdown->close connection to the server(stop bot)\n" +
                            "token -> display the current token\n" +
                            "listplugs -> list all available plugins\n" +
                            "dwplug [name] -> download plugin by name\n" +
                            "listlang -> list all available languages\n" +
                            "dwlang -> download language by name\n" +
                            "setlang [name] -> set language from the downloaded languages\n" +
                            "set-setting [setting.path] [value] -> set setting value"
                        );
                        Console.ForegroundColor = ConsoleColor.White;
                        break;
                    case "token":
                        if (File.Exists("./Data/Resources/DiscordBotCore.data"))
                            Console.WriteLine("Token: " + Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN", '='));
                        else Console.WriteLine("File could not be found. Please register token");
                        break;
                    default:
                        goto case "help";
                }

            }
        }

        private static void LoadPlugins(Boot discordbooter)
        {
            var loader = new PluginLoader(discordbooter.client);
            loader.onCMDLoad += (name, typeName, success, exception) =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                if (name == null || name.Length < 2)
                    name = typeName;
                if (success)
                    if (Language.ActiveLanguage == null)
                        Console.WriteLine("[CMD] Successfully loaded command : " + name);
                    else Console.WriteLine(Language.ActiveLanguage.FormatText(Language.ActiveLanguage.LanguageWords["COMMAND_LOAD_SUCCESS"], name));
                else
                    if (Language.ActiveLanguage == null)
                    Console.WriteLine("[CMD] Failed to load command : " + name + " because " + exception.Message);
                else
                    Console.WriteLine(Language.ActiveLanguage.FormatText(Language.ActiveLanguage.LanguageWords["COMMAND_LOAD_FAIL"], name, exception.Message));
                Console.ForegroundColor = ConsoleColor.Red;
            };
            loader.onEVELoad += (name, typeName, success, exception) =>
            {
                if (name == null || name.Length < 2)
                    name = typeName;
                Console.ForegroundColor = ConsoleColor.Green;
                if (success)
                    if (Language.ActiveLanguage == null)
                        Console.WriteLine("[EVENT] Successfully loaded event : " + name);
                    else
                        Console.WriteLine(Language.ActiveLanguage.FormatText(Language.ActiveLanguage.LanguageWords["EVENT_LOAD_SUCCESS"], name));
                else
                    if (Language.ActiveLanguage == null)
                    Console.WriteLine("[EVENT] Failed to load event : " + name + " because " + exception.Message);
                else
                    Console.WriteLine(Language.ActiveLanguage.FormatText(Language.ActiveLanguage.LanguageWords["EVENT_LOAD_FAIL"], name, exception.Message));
                Console.ForegroundColor = ConsoleColor.Red;
            };
            loader.LoadPlugins();

            PluginsLoaded = true;
        }

        /// <summary>
        /// Load the language from the specified file
        /// </summary>
        private static bool LoadLanguage()
        {
            string folder = Functions.langFolder;
            string langSettings = "./Data/Resources/Language.txt";
            if (!File.Exists(langSettings))
                File.WriteAllText(langSettings, "Language=English");
            //Load language from the specified file ...
            Language.ActiveLanguage = null;

            string langname = Functions.readCodeFromFile(langSettings, "Language", '=');
            if (langname == "English")
            {
                Language.ActiveLanguage = null;
                return true;
            }
            foreach (var file in Directory.GetFiles(folder))
            {
                if (Functions.readCodeFromFile(file, "LANGUAGE_NAME", '=') == langname)
                {
                    Language.ActiveLanguage = Language.CreateLanguageFromFile(file);

                    return true;
                }
            }

            if (Language.ActiveLanguage == null)
            {
                File.WriteAllText(langSettings, "Language=English");
                Console_Utilities.WriteColorText($"Failed to find language &r{langname} &c! Check available languages using command: &glistlang");

                return false;
            }

            return false;
        }


        public static void SetLanguage(string LanguageName)
        {
            string langSettings = Functions.dataFolder + "Language.txt";
            File.WriteAllText(langSettings, "Language=" + LanguageName);

            try
            {
                bool success = LoadLanguage();
                if (success)
                {
                    Console_Utilities.WriteColorText($"Language has been setted to: &g{LanguageName}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Console_Utilities.WriteColorText($"Could not find language &r{LanguageName}.");
                Functions.WriteErrFile(ex.ToString());
                File.WriteAllText(langSettings, "Language=English");
                LoadLanguage();
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
            string token = Functions.readCodeFromFile(Functions.dataFolder + "DiscordBotCore.data", "BOT_TOKEN", '=');
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
            string[] files = Directory.GetFiles(d);
            int fileNumb = files.Length;
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

            if (args.Length > 0)
                if (args[0] == "progress")
                {
                    Console_Utilities.ProgressBar bar = new Console_Utilities.ProgressBar(100, "Download");
                    for (int i = 0; i <= 100; i++)
                    {
                        bar.Update(i);
                        await Task.Delay(10);

                    }
                    return;
                }
                else if (args[0] == "test")
                {
                    return;
                }


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
                File.Delete(Functions.dataFolder + "Login.dat");
                Console.WriteLine("Logged out. Please restart the application !");
                return;
            }

            if (len >= 2 && args[0] == "--encrypt")
            {
                string s2e = args.MergeStrings(1);
                Console.WriteLine("MD5: " + await Cryptography.CreateMD5(s2e));
                Console.WriteLine("SHA356: " + await Cryptography.CreateSHA256(s2e));
                return;
            }

            if (len > 0 && (args.Contains("--cmd") || args.Contains("--args")))
            {
                if (args.Contains("lp") || args.Contains("loadplugins"))
                    loadPluginsOnStartup = true;
                if (args.Contains("listplugs"))
                    listPluginsAtStartup = true;
                if (args.Contains("listlang"))
                    listLanguagAtStartup = true;

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
