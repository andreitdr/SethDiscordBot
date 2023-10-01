using System;
using System.Threading.Tasks;
using DiscordBot.Bot.Actions.Extra;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace DiscordBot.Bot.Actions;

public class SettingsConfig : ICommandAction
{
    public string ActionName => "config";
    public string Description => "Change the settings of the bot";
    public string Usage => "config [options] <setting?> <value?>";
    public InternalActionRunType RunType => InternalActionRunType.ON_CALL;
    public Task Execute(string[] args)
    {
        if (args is null)
        {
            foreach (var settings in Config.AppSettings)
                Console.WriteLine(settings.Key + ": " + settings.Value);
            
            return Task.CompletedTask;
        }

        switch (args[0])
        {
            case "-s":
            case "set":
                if(args.Length < 3)
                    return Task.CompletedTask;
                SettingsConfigExtra.SetSettings(args[1],args[2..]);
                break;
            
            case "-r":
            case "remove":
                if(args.Length < 2)
                    return Task.CompletedTask;
                SettingsConfigExtra.RemoveSettings(args[1]);
                break;
            
            case "-a":
            case "add":
                if(args.Length < 3)
                    return Task.CompletedTask;
                SettingsConfigExtra.AddSettings(args[1], args[2..]);
                break;
            
            case "-h":
            case "-help":
                Console.WriteLine("Options:");
                Console.WriteLine("-s <settingName> <newValue>: Set a setting");
                Console.WriteLine("-r <settingName>: Remove a setting");
                Console.WriteLine("-a <settingName> <newValue>: Add a setting");
                Console.WriteLine("-h: Show this help message");
                break;
            
            default:
                Console.WriteLine("Invalid option");
                return Task.CompletedTask;
        }
        
        
        
        return Task.CompletedTask;
    }
}