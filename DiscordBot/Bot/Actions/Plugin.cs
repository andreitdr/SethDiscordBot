using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Bot.Actions.Extra;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;
using PluginManager.Others.Actions;

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
        new InternalActionOption("uninstall", "Uninstalls a plugin")
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
            case "refresh":
                await PluginMethods.RefreshPlugins(true);
                break;

            case "uninstall":
                string plugName = string.Join(' ', args, 1, args.Length-1);
                bool result = await Config.PluginsManager.MarkPluginToUninstall(plugName);
                if(result)
                    Console.WriteLine($"Marked to uninstall plugin {plugName}. Please restart the bot");
                break;

            case "list":
                await PluginMethods.List(Config.PluginsManager);
                break;
            case "load":
                if (pluginsLoaded)
                {
                    Config.Logger.Log("Plugins already loaded", typeof(ICommandAction), LogType.WARNING);
                    break;
                }

                if (Config.DiscordBot is null)
                {
                    Config.Logger.Log("DiscordBot is null", typeof(ICommandAction), LogType.WARNING);
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

                await PluginMethods.DownloadPlugin(Config.PluginsManager, pluginName);
                break;
        }
    }
}
