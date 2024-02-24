using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiscordBot.Utilities;
using PluginManager.Bot;
using PluginManager.Others;
using PluginManager.Others.Actions;
using PluginManager.UX;
using Spectre.Console;
using static PluginManager.Config;

namespace DiscordBot;

public class Program
{
    public static InternalActionManager internalActionManager;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    public static void Startup(string[] args)
    {
        PreLoadComponents(args).Wait();

        if (!AppSettings.ContainsKey("ServerID") || !AppSettings.ContainsKey("token") || !AppSettings.ContainsKey("prefix"))
            Installer.GenerateStartupConfig().Wait();

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

        ConsoleUtilities.WriteColorText($"Running on &m{(System.OperatingSystem.IsWindows() ? "Windows" : "Linux")}");
        Console.WriteLine("============================ LOG ============================");

        Console.ForegroundColor = ConsoleColor.White;
        try
        {
            var token         = AppSettings["token"];
            var prefix        = AppSettings["prefix"];
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
        }
        catch (Exception ex)
        {
            Logger.Log(ex.ToString(), source: typeof(Program), type: LogType.CRITICAL);
        }
    }

    /// <summary>
    ///     Handle user input arguments from the startup of the application
    /// </summary>
    private static async Task HandleInput()
    {
        await StartNoGui();
        try
        {
            internalActionManager = new InternalActionManager("./Data/Plugins", "*.dll");
            NoGUI();
        }
        catch (IOException ex)
        {
            if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
            {
                UxHandler.ShowMessageBox("SethBot", "An error occured while closing the bot last time. Please consider closing the bot using the &rexit&c method !\n" +
                                                    "There is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !", MessageBoxType.Error).Wait();
                
                
                Logger.Log("An error occured while closing the bot last time. Please consider closing the bot using the &rexit&c method !\n" +
                           "There is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !",
                    source: typeof(Program), type: LogType.ERROR
                );
            }
        }
    }

    private static async Task PreLoadComponents(string[] args)
    {
        await Initialize();

        Logger.OnLog += (sender, logMessage) =>
        {
            string messageColor = logMessage.Type switch
            {
                LogType.INFO     => "[green]",
                LogType.WARNING  => "[yellow]",
                LogType.ERROR    => "[red]",
                LogType.CRITICAL => "[red]",
                _                => "[white]"
            };

            if (logMessage.Message.Contains('['))
            {
                // If the message contains a tag, just print it as it is. No need to format it
                Console.WriteLine(logMessage.Message);
                return;
            }

            AnsiConsole.MarkupLine($"{messageColor}{logMessage.ThrowTime} {logMessage.Message} [/]");
        };

        AppSettings["Version"] = Assembly.GetExecutingAssembly().GetName().Version.ToString();
    }
}
