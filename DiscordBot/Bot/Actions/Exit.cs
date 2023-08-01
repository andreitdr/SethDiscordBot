using System;
using System.Threading.Tasks;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace DiscordBot.Bot.Actions;

public class Exit : ICommandAction
{
    public string                ActionName  => "exit";
    public string                Description => "Exits the bot and saves the config. Use exit help for more info.";
    public string                Usage       => "exit [help|force (-f)]";
    public InternalActionRunType RunType     => InternalActionRunType.ON_CALL;

    public async Task Execute(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            Config.Logger.Log("Exiting...", "Exit", isInternal: false);
            await Config.AppSettings.SaveToFile();
            await Config.Logger.SaveToFile();
            Environment.Exit(0);
        }
        else
        {
            switch ( args[0] )
            {
                case "help":
                    Console.WriteLine("Usage : exit [help|force]");
                    Console.WriteLine("help : Displays this message");
                    Console.WriteLine("force | -f : Exits the bot without saving the config");
                    break;

                case "-f":
                case "force":
                    Config.Logger.Log("Exiting (FORCE)...", "Exit", LogLevel.WARNING, false);
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Invalid argument !");
                    break;
            }
        }
    }
}
