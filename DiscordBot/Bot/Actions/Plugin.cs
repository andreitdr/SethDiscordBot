using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Bot.Actions.Extra;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;

namespace DiscordBot.Bot.Actions;

public class Plugin: ICommandAction
{
    private bool pluginsLoaded;
    public string ActionName => "plugin";
    public string Description => "Manages plugins. Use plugin help for more info.";
    public string Usage => "plugin <option!>";

    public IEnumerable<InternalActionOption> ListOfOptions => new List<InternalActionOption>
    {
        new InternalActionOption("help", "Displays this message"),
        new InternalActionOption("list", "Lists all plugins"),
        new InternalActionOption("load", "Loads all plugins"),
        new InternalActionOption("install", "Installs a plugin"),
        new InternalActionOption("refresh", "Refreshes the plugin list"),
        new InternalActionOption("uninstall", "Uninstalls a plugin"),
        new InternalActionOption("branch", "Sets a plugin option")
    };

    public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

    public async Task Execute(string[] args)
    {
        if (args is null || args.Length == 0 || args[0] == "help")
        {
            Console.WriteLine("Usage : plugin [help|list|load|install]");
            Console.WriteLine("help : Displays this message");
            Console.WriteLine("list : Lists all plugins");
            Console.WriteLine("load : Loads all plugins");
            Console.WriteLine("install : Installs a plugin");
            Console.WriteLine("refresh : Refreshes the plugin list");

            return;
        }

        switch (args[0])
        {
            case "branch":
                if (args.Length < 2)
                {
                    Console.WriteLine("Usage : plugin branch <option> <value>");
                    return;
                }

                string option = args[1];
                switch (option)
                {
                    case "set":
                    {
                        if (args.Length < 3)
                        {
                            Console.WriteLine("Usage : plugin branch set <value>");
                            return;
                        }

                        string value = string.Join(' ', args, 2, args.Length - 2);
                        Application.CurrentApplication.PluginManager.Branch = value;
                        Console.WriteLine($"Branch set to {value}");
                    }
                        break;
                    case "get":
                        Console.WriteLine($"Branch is set to {Application.CurrentApplication.PluginManager.Branch}");
                        break;

                    default:
                        Console.WriteLine("Invalid option");
                        break;
                }
                break;
            case "refresh":
                await PluginMethods.RefreshPlugins(true);
                break;

            case "uninstall":
                string plugName = string.Join(' ', args, 1, args.Length-1);
                bool result = await Application.CurrentApplication.PluginManager.MarkPluginToUninstall(plugName);
                if(result)
                    Console.WriteLine($"Marked to uninstall plugin {plugName}. Please restart the bot");
                break;
                
            case "list":
                await PluginMethods.List(Application.CurrentApplication.PluginManager);
                break;
            case "load":
                if (pluginsLoaded)
                {
                    Application.CurrentApplication.Logger.Log("Plugins already loaded", this, LogType.WARNING);
                    break;
                }

                if (Application.CurrentApplication.DiscordBotClient is null)
                {
                    Application.CurrentApplication.Logger.Log("DiscordBot is null", this, LogType.WARNING);
                    break;
                }

                pluginsLoaded = await PluginMethods.LoadPlugins(args);
                break;

            case "install":
                var pluginName = string.Join(' ', args, 1, args.Length - 1);
                if (string.IsNullOrEmpty(pluginName) || pluginName.Length < 2)
                {
                    Console.WriteLine("Please specify a plugin name");
                    Console.Write("Plugin name : ");
                    pluginName = Console.ReadLine();
                    if (string.IsNullOrEmpty(pluginName) || pluginName.Length < 2)
                    {
                        Console.WriteLine("Invalid plugin name");
                        break;
                    }
                }

                await PluginMethods.DownloadPlugin(Application.CurrentApplication.PluginManager, pluginName);
                break;
        }
    }
}
