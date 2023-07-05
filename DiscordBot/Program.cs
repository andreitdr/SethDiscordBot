using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using PluginManager.Bot;
using PluginManager.Online;
using PluginManager.Online.Helpers;
using PluginManager.Others;
using PluginManager.Others.Actions;
using static PluginManager.Config;

namespace DiscordBot;

public class Program
{
    public static Json<string, string>  URLs;
    public static InternalActionManager internalActionManager;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Startup(string[] args)
    {
        PreLoadComponents(args).Wait();

        if (!Data.ContainsKey("ServerID") || !Data.ContainsKey("token") ||
            Data["token"] == null ||
            (Data["token"]?.Length != 70 && Data["token"]?.Length != 59) ||
            !Data.ContainsKey("prefix") || Data["prefix"] == null ||
            Data["prefix"]?.Length != 1 ||
            (args.Length == 1 && args[0] == "/reset"))
            Installer.GenerateStartupConfig();

        HandleInput(args.ToList()).Wait();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static void NoGUI()
    {
#if DEBUG
        Console.WriteLine("Debug mode enabled");
        internalActionManager.Execute("plugin", "load").Wait(); // Load plugins at startup
#endif

        while (true)
        {
            string   cmd     = Console.ReadLine();
            string[] args    = cmd.Split(' ');
            string   command = args[0];
            args = args.Skip(1).ToArray();
            if (args.Length == 0)
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
        Console.WriteLine($"Git URL: {Data["GitURL"]}");

        Utilities.Utilities.WriteColorText(
                                           "&rRemember to close the bot using the ShutDown command (&ysd&r) or some settings won't be saved\n");
        Console.ForegroundColor = ConsoleColor.White;

        if (Data.ContainsKey("LaunchMessage"))
            Utilities.Utilities.WriteColorText(Data["LaunchMessage"]);


        Utilities.Utilities.WriteColorText(
                                           "Please note that the bot saves a backup save file every time you are using the shudown command (&ysd&c)");

        Console.WriteLine("Running on " + Functions.GetOperatingSystem());
        Console.WriteLine("============================ LOG ============================");

        try
        {
            var token = "";
#if DEBUG
            if (File.Exists("./Data/Resources/token.txt")) token = File.ReadAllText("./Data/Resources/token.txt");
            else token                                           = Data["token"];
#else
            token = Config.Data["token"];
#endif
            var prefix        = Data["prefix"];
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
            return discordbooter;
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString(), "Bot", LogLevel.ERROR);
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

        //Handle arguments here:

        if (args.Contains("--gui"))
        {
            // GUI not implemented yet 
            Console.WriteLine("GUI not implemented yet");
            return;
        }

        // Starting bot after all arguments are handled

        var b = await StartNoGui();
        try
        {
            internalActionManager = new InternalActionManager("./Data/Actions", "*.dll");
            await internalActionManager.Initialize();

            NoGUI();
        }
        catch (IOException ex)
        {
            if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
            {
                if (Data.ContainsKey("LaunchMessage"))
                    Data.Add("LaunchMessage",
                             "An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !");
                Logger
                    .Log("An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !",
                         "Bot", LogLevel.ERROR);
            }
        }
    }

    private static async Task PreLoadComponents(string[] args)
    {
        await Initialize();

        if (!Directory.Exists("./Data/Resources") || !File.Exists("./Data/Resources/URLs.json"))
            await Installer.SetupPluginDatabase();


        URLs = new Json<string, string>("./Data/Resources/URLs.json");

        Logger.LogEvent += (message, type, isInternal) =>
        {
            if (isInternal) return;
            if (type == LogLevel.INFO)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (type == LogLevel.WARNING)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (type == LogLevel.ERROR)
                Console.ForegroundColor = ConsoleColor.Red;
            else if(type == LogLevel.CRITICAL)
                Console.ForegroundColor = ConsoleColor.DarkRed;

            Console.WriteLine($"[{type.ToString()}] {message}");
            Console.ResetColor();
            
           

        };


        Console.WriteLine("Loading resources ...");

        if (Data.ContainsKey("DeleteLogsAtStartup"))
            if (Data["DeleteLogsAtStartup"] == "true")
                foreach (var file in Directory.GetFiles("./Output/Logs/"))
                    File.Delete(file);
        var OnlineDefaultKeys =
            await ServerCom.ReadTextFromURL(URLs["SetupKeys"]);


        Data["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();

        foreach (var key in OnlineDefaultKeys)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;
            var s = key.Split(' ');
            try
            {
                Data[s[0]] = s[1];
            }
            catch (Exception ex)
            {
                Logger.Log(ex.ToString(), "Bot", LogLevel.ERROR);
            }
        }


        var onlineSettingsList = await ServerCom.ReadTextFromURL(URLs["Versions"]);
        foreach (var key in onlineSettingsList)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;

            var s = key.Split(' ');
            switch (s[0])
            {
                case "CurrentVersion":
                    var currentVersion = Data["Version"];
                    var newVersion     = s[1];
                    if (new VersionString(newVersion) != new VersionString(newVersion))
                    {
                        Console.WriteLine("A new updated was found. Check the changelog for more information.");
                        var changeLog = await ServerCom.ReadTextFromURL(URLs["Changelog"]);
                        foreach (var item in changeLog)
                            Utilities.Utilities.WriteColorText(item);
                        Console.WriteLine("Current version: " + currentVersion);
                        Console.WriteLine("Latest version: " + newVersion);

                        Console.WriteLine("Download from here: https://github.com/andreitdr/SethDiscordBot/releases");

                        Console.WriteLine("Press any key to continue ...");
                        Console.ReadKey();
                    }

                    break;
            }
        }

        Console.Clear();
    }
}
