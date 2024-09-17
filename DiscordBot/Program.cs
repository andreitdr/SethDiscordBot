using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using DiscordBot.Bot.Actions.Extra;
using DiscordBot.Utilities;
using DiscordBotCore;
using DiscordBotCore.Bot;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;
using DiscordBotCore.Updater.Application;

using Spectre.Console;

namespace DiscordBot;

public class Program
{
    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    public static async Task Startup(string[] args)
    {
        await LoadComponents(args);
        await PrepareConsole();
        await PluginMethods.RefreshPlugins(false);
        await ConsoleInputHandler();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    private static async Task ConsoleInputHandler()
    {

        while (true)
        {
            var cmd     = Console.ReadLine();
            var args    = cmd.Split(' ');
            var command = args[0];
            args = args.Skip(1).ToArray();
            if (args.Length == 0)
                args = null;

            await Application.CurrentApplication.InternalActionManager.Execute(command, args); 
        }
    }

    /// <summary>
    ///     Start the bot without user interface
    /// </summary>
    /// <returns>Returns the bootloader for the Discord Bot</returns>
    private static async Task PrepareConsole()
    {
        AnsiConsole.MarkupLine($"[yellow]Running on version: {Assembly.GetExecutingAssembly().GetName().Version}[/]");
        AnsiConsole.MarkupLine("[yellow]Git SethBot: https://github.com/andreitdr/SethDiscordBot [/]");
        AnsiConsole.MarkupLine("[yellow]Git Plugins: https://github.com/andreitdr/SethPlugins [/]");

        AnsiConsole.MarkupLine("[yellow]Remember to close the bot using the shutdown command ([/][red]exit[/][yellow]) or some settings won't be saved[/]");
        AnsiConsole.MarkupLine($"[yellow]Running on [/][magenta]{(OperatingSystem.IsWindows() ? "Windows" : "Linux")}[/]");

        AnsiConsole.MarkupLine("[yellow]===== Seth Discord Bot =====[/]");

        try
        {
            var token  = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("token");
            var prefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("prefix");
            
            DiscordBotApplication discordApp = new (token, prefix);
            await discordApp.StartAsync();
        }
        catch (Exception ex)
        {
            Application.Logger.Log(ex.ToString(), typeof(Program), LogType.Critical);
        }
    }

    /// <summary>
    /// Load the bot components.
    /// </summary>
    /// <param name="args">The startup arguments</param>
    private static async Task LoadComponents(string[] args)
    {   
        await Application.CreateApplication(default);

        AppUpdater updater = new AppUpdater();
        Update? update = await updater.PrepareUpdate();
        if(update is not null)
        {
            await ConsoleUtilities.ExecuteTaskWithBuiltInProgress(updater.SelfUpdate, update, "Discord Bot Update");
            return;
        }
        
        void LogMessageFunction(string message, LogType logType)
        {
            string messageAsString = message;
            switch (logType)
            {
                case LogType.Info:
                    messageAsString = $"[green]{messageAsString} [/]";
                    break;
                case LogType.Warning:
                    messageAsString = $"[yellow]{messageAsString} [/]";
                    break;
                case LogType.Error:
                    messageAsString = $"[red]{messageAsString} [/]";
                    break;
                case LogType.Critical:
                    messageAsString = $"[red] [bold]{messageAsString} [/][/]";
                    break;

            }
            
            AnsiConsole.MarkupLine(messageAsString);
        }

        Application.Logger.SetOutFunction(LogMessageFunction);


        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID") ||
            !Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token") ||
            !Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix"))
            await Installer.GenerateStartupConfig();

        
    }
    
}
