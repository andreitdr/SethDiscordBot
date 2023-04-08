using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using PluginManager;
using PluginManager.Bot;
using PluginManager.Items;
using PluginManager.Online;
using PluginManager.Online.Helpers;
using PluginManager.Others;
using PluginManager.WindowManagement;

using OperatingSystem = PluginManager.Others.OperatingSystem;

namespace DiscordBot;

public class Program
{
    private static bool loadPluginsOnStartup;
    private static ConsoleCommandsHandler consoleCommandsHandler;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Startup(string[] args)
    {
        PreLoadComponents(args).Wait();

        if (!Config.Data.ContainsKey("ServerID") || !Config.Data.ContainsKey("token") ||
            Config.Data["token"] == null ||
            (Config.Data["token"]?.Length != 70 && Config.Data["token"]?.Length != 59) ||
            !Config.Data.ContainsKey("prefix") || Config.Data["prefix"] == null ||
            Config.Data["prefix"]?.Length != 1 ||
            (args.Length == 1 && args[0] == "/reset"))
        {
            GenerateStartUI();
        }

        HandleInput(args).Wait();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static void NoGUI()
    {
#if DEBUG
        Logger.WriteLine();
        Logger.WriteLine("Debug mode enabled");
        Logger.WriteLine();

#endif
        if (loadPluginsOnStartup)
            consoleCommandsHandler.HandleCommand("lp");

        while (true)
        {
            var cmd = Console.ReadLine();
            if (!consoleCommandsHandler.HandleCommand(cmd!
#if DEBUG
                                                    , false
#endif

            ) && cmd.Length > 0)
                Logger.WriteLine("Failed to run command " + cmd);
        }
    }

    /// <summary>
    ///     Start the bot without user interface
    /// </summary>
    /// <returns>Returns the boot loader for the Discord Bot</returns>
    private static async Task<Boot> StartNoGui()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        var startupMessageList =
            await ServerCom.ReadTextFromURL(
                "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/StartupMessage");

        foreach (var message in startupMessageList)
            Logger.WriteLine(message);

        Logger.WriteLine(
            $"Running on version: {Assembly.GetExecutingAssembly().GetName().Version}");
        Logger.WriteLine($"Git URL: {Config.Data["GitURL"]}");

        Utilities.WriteColorText(
            "&rRemember to close the bot using the ShutDown command (&ysd&r) or some settings won't be saved\n");
        Console.ForegroundColor = ConsoleColor.White;

        if (Config.Data.ContainsKey("LaunchMessage"))
            Utilities.WriteColorText(Config.Data["LaunchMessage"]);


        Utilities.WriteColorText(
            "Please note that the bot saves a backup save file every time you are using the shudown command (&ysd&c)");

        Logger.WriteLine();
        Logger.WriteLine("Running on " + Functions.GetOperatingSystem().ToString());
        Logger.WriteLine("============================ LOG ============================");

        try
        {
            string token = "";
#if DEBUG
            if (File.Exists("./Data/Resources/token.txt")) token = File.ReadAllText("./Data/Resources/token.txt");
            else token = Config.Data["token"];
#else
            token = Config.Data["token"];
#endif
            var prefix = Config.Data["prefix"];
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
            return discordbooter;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            return null;
        }
    }

    /// <summary>
    ///     Handle user input arguments from the startup of the application
    /// </summary>
    /// <param name="args">The arguments</param>
    private static async Task HandleInput(string[] args)
    {
        var len = args.Length;

        var b = await StartNoGui();
        consoleCommandsHandler = new ConsoleCommandsHandler(b.client);

        if (len > 0 && args[0] == "/remplug")
        {
            var plugName = string.Join(' ', args, 1, args.Length - 1);
            Logger.WriteLine("Starting to remove " + plugName);
            await ConsoleCommandsHandler.ExecuteCommad("remplug " + plugName);
            loadPluginsOnStartup = true;
        }



        var mainThread = new Thread(() =>
        {
            try
            {
                NoGUI();
            }
            catch (IOException ex)
            {
                if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
                {
                    if (Config.Data.ContainsKey("LaunchMessage"))
                        Config.Data.Add("LaunchMessage",
                                             "An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !");
                    Logger.WriteErrFile(ex.ToString());
                }
            }
        });
        mainThread.Start();
    }

    private static async Task PreLoadComponents(string[] args)
    {
        await Config.Initialize(true);


        Logger.WriteLine("Loading resources ...");
        var main = new Utilities.ProgressBar(ProgressBarType.NO_END);
        main.Start();

        if (Config.Data.ContainsKey("DeleteLogsAtStartup"))
            if (Config.Data["DeleteLogsAtStartup"] == "true")
                foreach (var file in Directory.GetFiles("./Output/Logs/"))
                    File.Delete(file);
        var OnlineDefaultKeys =
            await ServerCom.ReadTextFromURL(
                "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/SetupKeys");


        Config.Data["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        foreach (var key in OnlineDefaultKeys)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;
            var s = key.Split(' ');
            try
            {
                Config.Data[s[0]] = s[1];
            }
            catch (Exception ex)
            {
                Logger.WriteErrFile(ex.Message);
            }
        }


        var onlineSettingsList =
            await ServerCom.ReadTextFromURL(
                "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/OnlineData");
        main.Stop("Loaded online settings. Loading updates ...");
        foreach (var key in onlineSettingsList)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;

            var s = key.Split(' ');
            switch (s[0])
            {
                case "CurrentVersion":
                    var newVersion = s[1];
                    var currentVersion = Config.Data["Version"];
                    if (!newVersion.Equals(currentVersion))
                    {
                        var nVer = new VersionString(newVersion.Substring(2));
                        var cVer = new VersionString((Config.Data["Version"]).Substring(2));
                        if (cVer > nVer)
                        {
                            Config.Data["Version"] = "1." + cVer.ToShortString() + " (Beta)";
                            break;
                        }
                        
                        if (OperatingSystem.WINDOWS == Functions.GetOperatingSystem())
                        {
                            Console.Clear();
                            Logger.WriteLine("A new update was found !");
                            Logger.WriteLine("Run the launcher to update");
                            Logger.WriteLine("Current version: " + currentVersion);
                            Logger.WriteLine("Latest version: " + s[1]);

                            File.WriteAllText("version.txt", currentVersion);

                            await Task.Delay(3000);

                            break;
                        }

                        Console.Clear();
                        Console.ForegroundColor = ConsoleColor.Red;
                        Logger.WriteLine("A new version of the bot is available !");
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Logger.WriteLine("Current version : " +
                                         Assembly.GetExecutingAssembly().GetName().Version.ToString());
                        Console.ForegroundColor = ConsoleColor.Green;
                        Logger.WriteLine("New version : " + newVersion);
                        Console.ForegroundColor = ConsoleColor.White;

                        File.WriteAllText("version.txt", newVersion);

                        Logger.WriteLine("Changelog :");

                        List<string> changeLog = await ServerCom.ReadTextFromURL(
                            "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/VersionData/DiscordBot");
                        foreach (var item in changeLog)
                            Utilities.WriteColorText(item);
                        Logger.WriteLine("Do you want to update the bot ? (y/n)");
                        if (Console.ReadKey().Key == ConsoleKey.Y)
                        {
                            var url =
                                $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}/net6.0_linux.zip";
                            if (Logger.isConsole)
                                Console.SetCursorPosition(0, Console.CursorTop);
                            Logger.WriteLine($"executing: download_file {url}");

                            await ServerCom.DownloadFileAsync(url, "./update.zip", new Progress<float>(percent => { Logger.Write($"\rProgress: {percent}%        "); }));
                            await File.WriteAllTextAsync("Install.sh",
                                                         "#!/bin/bash\nunzip -qq -o update.zip \nrm update.zip\nchmod a+x DiscordBot");
                            Logger.WriteLine();

                            try
                            {
                                Logger.WriteLine("executing: chmod a+x Install.sh");
                                Process.Start("chmod", "a+x Install.sh").WaitForExit();
                                Process.Start("Install.sh").WaitForExit();

                                Logger.WriteLine("executing: rm Install.sh");
                                Process.Start("rm", "Install.sh").WaitForExit();

                                Logger.WriteLine("The new version of the bot has been installed.");
                                Logger.WriteLine("Please restart the bot.");
                                Environment.Exit(0);
                            }
                            catch (Exception ex)
                            {
                                Logger.WriteErrFile(ex.Message);
                                if (ex.Message.Contains("Access de"))
                                    Logger.WriteLine("Please run the bot as root.");
                            }


                        }
                    }

                    break;
                case "LauncherVersion":
                    var updaternewversion = s[1];
                    //File.WriteAllText(updaternewversion + ".txt", updaternewversion);
                    if (Functions.GetOperatingSystem() == OperatingSystem.LINUX)
                        break;

                    Directory.CreateDirectory(Functions.dataFolder + "Applications");
                    if (!Config.Data.ContainsKey("LauncherVersion"))
                        Config.Data["LauncherVersion"] = "0.0.0.0";
                    if (Config.Data["LauncherVersion"] != updaternewversion ||
                        !File.Exists("./Launcher.exe"))
                    {
                        Console.Clear();
                        Logger.WriteLine("Installing a new Launcher ...\nDo NOT close the bot during update !");
                        var bar = new Utilities.ProgressBar(ProgressBarType.NO_END);
                        bar.Start();
                        await ServerCom.DownloadFileNoProgressAsync(
                            "https://github.com/Wizzy69/installer/releases/download/release-1-discordbot/Launcher.exe",
                            $"./Launcher.exe");
                        //await ArchiveManager.ExtractArchive("./Updater.zip", "./", null,
                        //                                    UnzipProgressType.PercentageFromTotalSize);
                        Config.Data["LauncherVersion"] = updaternewversion;
                        // File.Delete("Updater.zip");
                        bar.Stop("The launcher has been updated !");
                        Console.Clear();
                    }

                    break;
            }
        }

        Console.Clear();
    }

    public static void GenerateStartUI()
    {
        InputBox box = new InputBox();
        box.Title = "Discord Bot Config - " + Assembly.GetExecutingAssembly().GetName().Version;
        box.Message = "Let's setup the bot. Please go through this setup so that you can run the bot.\n\n";
        box.AddLabel("Note:All the information collected here will only be stored locally (on your machine) at the following path: ", TextType.WARNING);
        box.AddLabel("<executable path>/Data/Resources/Config.json", TextType.SUCCESS);

        box.AddOption("Bot Token", (token) => {
            if (token.Length != 70 && token.Length != 59)
            {
                Console.WriteLine("The token is invalid !");
                return false;
            }

            return true;
        });

        box.AddOption("Bot prefix (should be one character long)", (prefix) => {
            if (int.TryParse(prefix, out int value))
            {
                Console.WriteLine("The prefix can not be a number");
                return false;
            }

            if (prefix.Length != 1)
            {
                Console.WriteLine("The bot does not support longer prefixes");
                return false;
            }

            return true;

        });


        box.AddOption("Server ID (Optional => Press ENTER to leave empty)\nIf you let the Server ID field option empty, some plugins may not work", (servId) => {
            if (servId.Length != 18 && servId.Length > 0)
            {
                Console.WriteLine("The Server ID is invalid");
                return false;
            }

            return true;
        });

        List<string> result = box.Show();

        Config.Data.Add("ServerID", result[2]);
        Config.Data.Add("token", result[0]);
        Config.Data.Add("prefix", result[1]);
    }
}