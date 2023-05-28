using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

using PluginManager;
using PluginManager.Bot;
using PluginManager.Online;
using PluginManager.Online.Helpers;
using PluginManager.Others;

using DiscordBot.Utilities;
using Microsoft.VisualBasic.CompilerServices;
using OperatingSystem = PluginManager.Others.OperatingSystem;
using static PluginManager.Config;

namespace DiscordBot;

public class Program
{
    public static Json<string, string> URLs;
    private static bool loadPluginsOnStartup = false;
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
            Installer.GenerateStartupConfig();
        }
        
        HandleInput(args.ToList()).Wait(); 
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static void NoGUI()
    {
#if DEBUG
        Console.WriteLine("Debug mode enabled");

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
                Console.WriteLine("Failed to run command " + cmd);
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
            await ServerCom.ReadTextFromURL(URLs["StartupMessage"]);

        foreach (var message in startupMessageList)
            Console.WriteLine(message);

        Console.WriteLine(
            $"Running on version: {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine($"Git URL: {Config.Data["GitURL"]}");

        Utilities.Utilities.WriteColorText(
            "&rRemember to close the bot using the ShutDown command (&ysd&r) or some settings won't be saved\n");
        Console.ForegroundColor = ConsoleColor.White;

        if (Config.Data.ContainsKey("LaunchMessage"))
            Utilities.Utilities.WriteColorText(Config.Data["LaunchMessage"]);


        Utilities.Utilities.WriteColorText(
            "Please note that the bot saves a backup save file every time you are using the shudown command (&ysd&c)");

        Console.WriteLine("Running on " + Functions.GetOperatingSystem().ToString());
        Console.WriteLine("============================ LOG ============================");

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
            Config.Logger.Log(ex.ToString(), "Bot", LogLevel.ERROR);
            return null;
        }
    }

    /// <summary>
    ///     Handle user input arguments from the startup of the application
    /// </summary>
    /// <param name="args">The arguments</param>
    private static async Task HandleInput(List<string> args)
    {

        Console.WriteLine("Loading Core ...");

        var b = await StartNoGui();
        consoleCommandsHandler = new ConsoleCommandsHandler(b.client);
        try
        {
            if(args.Contains("--gui"))
            {
                 // GUI not implemented yet 
                 Console.WriteLine("GUI not implemented yet");
                 return;
            }
            NoGUI();
        }
        catch (IOException ex)
        {
            if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
            {
                if (Config.Data.ContainsKey("LaunchMessage"))
                    Config.Data.Add("LaunchMessage",
                                         "An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !");
                Config.Logger.Log("An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !", "Bot", LogLevel.ERROR);
            }
        }
        return;
    }

    private static async Task PreLoadComponents(string[] args)
    {

        await Config.Initialize();

        if (!Directory.Exists("./Data/Resources") || !File.Exists("./Data/Resources/URLs.json"))
        {
            await Installer.SetupPluginDatabase();
        }


        URLs = new Json<string, string>("./Data/Resources/URLs.json");

        Config.Logger.LogEvent += (message, type) => { Console.WriteLine(message); };


        Console.WriteLine("Loading resources ...");
        var main = new Utilities.Utilities.ProgressBar(ProgressBarType.NO_END);
        main.Start();

        if (Config.Data.ContainsKey("DeleteLogsAtStartup"))
            if (Config.Data["DeleteLogsAtStartup"] == "true")
                foreach (var file in Directory.GetFiles("./Output/Logs/"))
                    File.Delete(file);
        var OnlineDefaultKeys =
            await ServerCom.ReadTextFromURL(URLs["SetupKeys"]);


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
                Config.Logger.Log(ex.ToString(), "Bot", LogLevel.ERROR);
            }
        }


        var onlineSettingsList = await ServerCom.ReadTextFromURL(URLs["Versions"]);
        main.Stop("Loaded online settings. Loading updates ...");
        foreach (var key in onlineSettingsList)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;

            var s = key.Split(' ');
            switch (s[0])
            {
                case "CurrentVersion":
                    var currentVersion = Config.Data["Version"];
                    var newVersion = s[1];
                    if(newVersion != currentVersion)
                    {
                        Console.WriteLine("A new updated was found. Check the changelog for more information.");
                        Console.WriteLine("Current version: " + currentVersion);
                        Console.WriteLine("Latest version: " + newVersion);

                        Console.WriteLine("Woud you like to go to the download page ? (y/n)");
                        if (Console.ReadKey().Key == ConsoleKey.Y)
                        {
                            Process.Start($"https://github.com/Wizzy69/SethDiscordBot/releases/tag/v{newVersion}");
                        }

                        Console.WriteLine("Press any key to continue ...");
                        Console.ReadKey();
                    }
                break;
            //     case "CurrentVersion":
            //         var newVersion = s[1];
            //         var currentVersion = Config.Data["Version"];

            //         if (!newVersion.Equals(currentVersion))
            //         {

            //             var nVer = new VersionString(newVersion.Substring(2));
            //             var cVer = new VersionString((Config.Data["Version"]).Substring(2));
            //             if (cVer > nVer)
            //             {
            //                 Config.Data["Version"] = "1." + cVer.ToShortString() + " (Beta)";
            //                 break;
            //             }

            //             if (OperatingSystem.WINDOWS == Functions.GetOperatingSystem())
            //             {
            //                 Console.Clear();
            //                 Console.WriteLine("A new update was found !");
            //                 Console.WriteLine("Run the launcher to update");
            //                 Console.WriteLine("Current version: " + currentVersion);
            //                 Console.WriteLine("Latest version: " + s[1]);

            //                 File.WriteAllText("version.txt", currentVersion);

            //                 await Task.Delay(3000);

            //                 break;
            //             }

            //             if(OperatingSystem.LINUX == Functions.GetOperatingSystem())
            //             {
            //                 Console.WriteLine("A new update was found !");
            //                 Console.WriteLine("Run the launcher to update");
            //             }
            //             Console.Clear();
            //             Console.ForegroundColor = ConsoleColor.Red;
            //             Console.WriteLine("A new version of the bot is available !");
            //             Console.ForegroundColor = ConsoleColor.Yellow;
            //             Console.WriteLine("Current version : " +
            //                              Assembly.GetExecutingAssembly().GetName().Version.ToString());
            //             Console.ForegroundColor = ConsoleColor.Green;
            //             Console.WriteLine("New version : " + newVersion);
            //             Console.ForegroundColor = ConsoleColor.White;

            //             File.WriteAllText("version.txt", newVersion);

            //             Console.WriteLine("Changelog :");

            //             List<string> changeLog = await ServerCom.ReadTextFromURL(URLs["Changelog"]);
            //             foreach (var item in changeLog)
            //                 Utilities.Utilities.WriteColorText(item);
            //             Console.WriteLine("Do you want to update the bot ? (y/n)");
            //             if (Console.ReadKey().Key == ConsoleKey.Y)
            //             {
            //                 var url = URLs["LinuxBot"].Replace("{0}", newVersion);
            //                 Config.Logger.Log($"executing: download_file {url}");

            //                 await ServerCom.DownloadFileAsync(url, "./update.zip", new Progress<float>(percent => { Console.WriteLine($"\rProgress: {percent}%        "); }));
            //                 await File.WriteAllTextAsync("Install.sh",
            //                                              "#!/bin/bash\nunzip -qq -o update.zip \nrm update.zip\nchmod a+x DiscordBot");

            //                 try
            //                 {
            //                     Console.WriteLine("executing: chmod a+x Install.sh");
            //                     Process.Start("chmod", "a+x Install.sh").WaitForExit();
            //                     Process.Start("Install.sh").WaitForExit();

            //                     Console.WriteLine("executing: rm Install.sh");
            //                     Process.Start("rm", "Install.sh").WaitForExit();

            //                     Config.Logger.Log("The new version of the bot has been installed.");
            //                     Console.WriteLine("Please restart the bot.");
            //                     Environment.Exit(0);
            //                 }
            //                 catch (Exception ex)
            //                 {
            //                     Config.Logger.Log(ex.Message, "Updater", LogLevel.ERROR);
            //                     if (ex.Message.Contains("Access de"))
            //                         Config.Logger.Log("Please run the bot as sudo.");
            //                 }


            //             }
            //         }

            //         break;
            //     case "LauncherVersion":
            //         var updaternewversion = s[1];
            //         //File.WriteAllText(updaternewversion + ".txt", updaternewversion);
            //         if (Functions.GetOperatingSystem() == OperatingSystem.LINUX)
            //             break;

            //         Directory.CreateDirectory(Functions.dataFolder + "Applications");
            //         if (!Config.Data.ContainsKey("LauncherVersion"))
            //             Config.Data["LauncherVersion"] = "0.0.0.0";
            //         if (Config.Data["LauncherVersion"] != updaternewversion ||
            //             !File.Exists("./Launcher.exe"))
            //         {
            //             Console.Clear();
            //             Console.WriteLine("Installing a new Launcher ...\nDo NOT close the bot during update !");
            //             var bar = new Utilities.Utilities.ProgressBar(ProgressBarType.NO_END);
            //             bar.Start();
            //             await ServerCom.DownloadFileAsync(URLs["WindowsLauncher"], $"./Launcher.exe", null);
            //             //await ArchiveManager.ExtractArchive("./Updater.zip", "./", null,
            //             //                                    UnzipProgressType.PercentageFromTotalSize);
            //             Config.Data["LauncherVersion"] = updaternewversion;
            //             // File.Delete("Updater.zip");
            //             bar.Stop("The launcher has been updated !");
            //             Console.Clear();
            //         }

            //         break;
             }
        }

        Console.Clear();
    }
}