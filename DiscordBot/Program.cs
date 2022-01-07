using Discord;
using System;
using System.IO;
using System.Threading.Tasks;

using PluginManager.Core;
using PluginManager.Others;

using PluginManager.Loaders;
using PluginManager.LanguageSystem;
using PluginManager.Online;
namespace DiscordBot
{
    public class Program
    {
        private static PluginsManager manager = new PluginsManager("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Plugins");
        private static LanguageManager languageManager = new LanguageManager("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Languages");

        private static bool loadPluginsOnStartup = false;

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        [Obsolete]

        public static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AppendPrivatePath(".\\Requirements");
            Console.Clear();
            Directory.CreateDirectory("./Data/Resources");
            Directory.CreateDirectory("./Data/Languages");
            Directory.CreateDirectory("./Data/Plugins/Commands");
            Directory.CreateDirectory("./Data/Plugins/Events");
            if (!File.Exists("./Data/Resources/DiscordBotCore.data") || Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN", '\t').Length != 59)
            {
                File.WriteAllText("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN\ttoken\nBOT_PREFIX\t!\n");
                while (true)
                {
                    Console.WriteLine("Please insert your token: ");
                    Console.Write("TOKEN: ");
                    string botToken = Console.ReadLine();
                    if (botToken.Length == 59)
                    {
                        string prefix = Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_PREFIX",
                            '\t');
                        if (prefix == String.Empty || prefix == null)
                            prefix = "!";
                        File.WriteAllText("./Data/Resources/DiscordBotCore.data", $"BOT_TOKEN\t{botToken}\nBOT_PREFIX\t{prefix}\n");
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
            if (loadPluginsOnStartup) LoadPlugins(discordbooter);
            while (true)
            {
                Console.ForegroundColor = ConsoleColor.White;
                Console.Write('$');
                string[] data = Console.ReadLine().Split(' ');
                if (data[0].Length < 2) continue;
                switch (data[0])
                {
                    case "shutdown":
                    case "sd":
                        if (discordbooter.client.ConnectionState == ConnectionState.Connected)
                            await discordbooter.ShutDown().ContinueWith(t => { Environment.Exit(0); });
                        break;

                    case "listplugs":
                        await manager.ListAvailablePlugins();
                        break;

                    case "dwplug":
                        string name = data.MergeStrings(1);
                        string[] info = await manager.GetPluginLinkByName(name);
                        if (info[1] == null) // link is null
                        {
                            if (name == "")
                            {
                                Functions.WriteColorText($"Name is invalid");
                                break;
                            }
                            Functions.WriteColorText($"Failed to find plugin &b{name} &c! Use &glistplugs &ccommand to display all available plugins !");
                            break;

                        }
                        Downloader dw = new Downloader(name + ".dll", info[1]);
                        await dw.DownloadFileAsync("./Data/Plugins/", info[0]);
                        break;
                    case "setlang":
                        if (data.Length == 2)
                            SetLanguage(data[1]);
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
                        if (link[0] == null)
                        {
                            if (Lname == "")
                            {
                                Functions.WriteColorText($"Name is invalid");
                                break;
                            }
                            Functions.WriteColorText("Failed to find language &b" + Lname + " &c! Use &glistlang &ccommand to display all available languages !");
                            break;
                        }
                        if (link[1].Contains("CrossPlatform") || link[1].Contains("cp"))
                        {
                            Downloader dwn = new Downloader(Lname + ".lng", link[0]);
                            await dwn.DownloadFileAsync(Functions.langFolder);
                        }
                        else Functions.WriteColorText("The language you are trying to download (&b" + Lname + "&c) is not compatible with the version of this bot. User &glistlang &ccommand in order to see all available languages for your current version !\n" + link[1]);
                        break;
                    case "loadplugins":
                    case "lp":
                        LoadPlugins(discordbooter);
                        break;
                    case "help":
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine(
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
                            Console.WriteLine("Token: " + Functions.readCodeFromFile("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN", '\t'));
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

            string langname = File.ReadAllText(langSettings).Split('=')[1];
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
                Functions.WriteColorText($"Failed to find language &r{langname} &c! Check available languages using command: &glistlang");

                return false;
            }

            return false;
        }

        public static void SetLanguage(string LanguageName)
        {

            string folder = Functions.langFolder;
            string langSettings = Functions.dataFolder + "Language.txt";
            File.WriteAllText(langSettings, "Language=" + LanguageName);

            try
            {
                bool success = LoadLanguage();
                if (success)
                {
                    Functions.WriteColorText($"Language has been setted to: &g{LanguageName}");
                    return;
                }
            }
            catch (Exception ex)
            {
                Functions.WriteColorText($"Could not find language &r{LanguageName}.");
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
            Console.WriteLine("Discord BOT for Cross Platform\n\nCreated by: Wizzy\nDiscord: Wizzy#9181\nCommands:");
            Console.WriteLine(
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
            Console.WriteLine("============================ Discord BOT - Cross Platform ============================");
            string token =
                Functions.readCodeFromFile((Functions.dataFolder + "DiscordBotCore.data"), "BOT_TOKEN",
                                           '\t');
            string prefix = Functions.readCodeFromFile((Functions.dataFolder + "DiscordBotCore.data"),
                                                       "BOT_PREFIX",
                                                       '\t');

            Console.WriteLine("Detected prefix: " + prefix);
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
        /// Replace text in the file
        /// </summary>
        /// <param name="file">The file location (path)</param>
        /// <param name="code">The setting key code where to replace</param>
        /// <param name="value">The new value</param>
        /// <exception cref="FileNotFoundException">If the <paramref name="file"/> does not exist, then this error is thrown</exception>
        private static void ReplaceText(string file, string code, string value)
        {
            try
            {
                var f = false;
                string[] text = File.ReadAllLines(file);
                foreach (string line in text)
                    if (line.StartsWith(code))
                    {
                        line.Replace(line.Split('\t')[1], value);
                        f = true;
                    }

                if (f)
                    File.WriteAllLines(@"./Data/Resources/DiscordBotCore.data", text);
                else throw new FileNotFoundException();
            }
            catch (FileNotFoundException)
            {
                File.AppendAllText(file, code + "\t" + value + "\n");
            }
        }

        /// <summary>
        /// Handle user input arguments from the startup of the application
        /// </summary>
        /// <param name="args">The arguments</param>
        private static async Task HandleInput(string[] args)
        {
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

            if (len == 2 && args[0] == "--encrypt")
            {
                Console.WriteLine("MD5: " + await Cryptography.CreateMD5(args[1]));
                System.Console.WriteLine("SHA356: " + await Cryptography.CreateSHA256(args[1]));
                return;
            }

            if (len == 1 && args[0] == "--execute:lp")
            {
                loadPluginsOnStartup = true;
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
