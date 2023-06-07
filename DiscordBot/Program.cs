using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Linq;

using PluginManager;
using PluginManager.Bot;
using PluginManager.Online;
using PluginManager.Online.Helpers;
using PluginManager.Others;

using DiscordBot.Utilities;
using static PluginManager.Config;
using PluginManager.Interfaces;

namespace DiscordBot;

public class Program
{
    public static Json<string, string> URLs;
    public static PluginManager.Others.Actions.InternalActionManager internalActionManager;

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

        while (true)
        {
            var cmd = Console.ReadLine();
            string[] args = cmd.Split(' ');
            string command = args[0];
            args = args.Skip(1).ToArray();
            if(args.Length == 0)
                args = null;

            internalActionManager.Execute(command, args).Wait(); // Execute the command
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
        try
        {
            if(args.Contains("--gui"))
            {
                 // GUI not implemented yet 
                 Console.WriteLine("GUI not implemented yet");
                 return;
            }

            internalActionManager = new PluginManager.Others.Actions.InternalActionManager("./Data/Actions", "*.dll");
            await internalActionManager.Initialize();

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
                    if(new VersionString(newVersion) != new VersionString(newVersion))
                    {
                        Console.WriteLine("A new updated was found. Check the changelog for more information.");
                        List<string> changeLog = await ServerCom.ReadTextFromURL(URLs["Changelog"]);
                        foreach (var item in changeLog)
                            Utilities.Utilities.WriteColorText(item);
                        Console.WriteLine("Current version: " + currentVersion);
                        Console.WriteLine("Latest version: " + newVersion);

                        Console.WriteLine($"Download from here: https://github.com/Wizzy69/SethDiscordBot/releases/tag/v{newVersion}");

                        Console.WriteLine("Press any key to continue ...");
                        Console.ReadKey();
                    }
                break;
             }
        }

        Console.Clear();
        
    }
}