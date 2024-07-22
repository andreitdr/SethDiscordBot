using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBot.Bot.Actions;

public class Exit: ICommandAction
{
    public string ActionName => "exit";
    public string Description => "Exits the bot and saves the config. Use exit help for more info.";
    public string Usage => "exit <option?>";
    public IEnumerable<InternalActionOption> ListOfOptions => new List<InternalActionOption>
    {
        new InternalActionOption("help", "Displays this message"),
        new InternalActionOption("force | -f", "Exits the bot without saving the config")
    };
    public InternalActionRunType RunType => InternalActionRunType.OnCall;
    
    public bool RequireOtherThread => false;

    public async Task Execute(string[] args)
    {
        if (args is null || args.Length == 0)
        {
            Application.CurrentApplication.Logger.Log("Exiting...", this, LogType.Warning);
            await Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();
            Environment.Exit(0);
        }
        else
        {
            switch (args[0])
            {
                case "help":
                    Console.WriteLine("Usage : exit [help|force]");
                    Console.WriteLine("help : Displays this message");
                    Console.WriteLine("force | -f : Exits the bot without saving the config");
                    break;

                case "-f":
                case "force":
                    Application.CurrentApplication.Logger.Log("Exiting (FORCE)...", this, LogType.Warning);
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Invalid argument !");
                    break;
            }
        }
    }
}
