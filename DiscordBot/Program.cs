using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiscordBot.Utilities;
using PluginManager.Bot;
using PluginManager.Others;
using PluginManager.Others.Actions;
using static PluginManager.Config;

namespace DiscordBot;

public class Program
{
    public static InternalActionManager internalActionManager;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    public static void Startup(string[] args)
    {
        PreLoadComponents(args).Wait();
        
        if (!AppSettings.ContainsKey("ServerID") || !AppSettings.ContainsKey("token") || !AppSettings.ContainsKey("prefix"))
                Installer.GenerateStartupConfig();

        HandleInput().Wait();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static void NoGUI()
    {
        internalActionManager.Initialize().Wait();
        internalActionManager.Execute("plugin", "load").Wait();
        internalActionManager.Refresh().Wait();
        
        
        while (true)
        {
            var cmd     = Console.ReadLine();
            var args    = cmd.Split(' ');
            var command = args[0];
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
    private static async Task StartNoGui()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        Console.WriteLine($"Running on version: {Assembly.GetExecutingAssembly().GetName().Version}");
        Console.WriteLine("Git SethBot: https://github.com/andreitdr/SethDiscordBot");
        Console.WriteLine("Git Plugins: https://github.com/andreitdr/SethPlugins");
        
        ConsoleUtilities.WriteColorText("&rRemember to close the bot using the ShutDown command (&yexit&r) or some settings won't be saved");

        ConsoleUtilities.WriteColorText($"Running on &m{Functions.GetOperatingSystem()}");
        Console.WriteLine("============================ LOG ============================");
        
        Console.ForegroundColor = ConsoleColor.White;
        try
        {
            var token = AppSettings["token"];
            var prefix        = AppSettings["prefix"];
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
        }
        catch ( Exception ex )
        {
            Logger.Log(ex.ToString(), "Bot", LogLevel.ERROR);
        }
    }

    /// <summary>
    ///     Handle user input arguments from the startup of the application
    /// </summary>
    /// <param name="args">The arguments</param>
    private static async Task HandleInput()
    {
        await StartNoGui();
        try
        {
            internalActionManager = new InternalActionManager("./Data/Plugins", "*.dll");
            NoGUI();
        }
        catch ( IOException ex )
        {
            if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
            {
                if (AppSettings.ContainsKey("LaunchMessage"))
                    AppSettings.Add("LaunchMessage",
                                    "An error occured while closing the bot last time. Please consider closing the bot using the &rexit&c method !\n" +
                                    "There is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !");
                
                Logger.Log("An error occured while closing the bot last time. Please consider closing the bot using the &rexit&c method !\n" +
                           "There is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !", 
                           "Bot", LogLevel.ERROR);
            }
        }
    }

    private static async Task PreLoadComponents(string[] args)
    {
        await Initialize();
        
        Logger.LogEvent += (message, type, isInternal) =>
        {
            if (type == LogLevel.INFO)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (type == LogLevel.WARNING)
                Console.ForegroundColor = ConsoleColor.DarkYellow;
            else if (type == LogLevel.ERROR)
                Console.ForegroundColor = ConsoleColor.Red;
            else if (type == LogLevel.CRITICAL)
                Console.ForegroundColor = ConsoleColor.DarkRed;

            Console.WriteLine($"[{type.ToString()}] {message}");
            Console.ResetColor();
        };
        
        AppSettings["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
