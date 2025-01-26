using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using DiscordBot.Utilities;

using DiscordBotCore;
using DiscordBotCore.Loaders;
using DiscordBotCore.Online;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;

using Spectre.Console;

namespace DiscordBot.Bot.Actions.Extra;

internal static class PluginMethods
{

    internal static async Task List()
    {
        Console.WriteLine($"Fetching plugin list ...");

        var onlinePlugins = await ConsoleUtilities.ExecuteWithProgressBar(Application.CurrentApplication.PluginManager.GetPluginsList(), "Reading remote database");
        var installedPlugins = await ConsoleUtilities.ExecuteWithProgressBar(Application.CurrentApplication.PluginManager.GetInstalledPlugins(), "Reading local database ");
        TableData tableData = new(["Name", "Description", "Author", "Latest Version", "Installed Version"]);

        foreach (var onlinePlugin in onlinePlugins)
        {
            bool isInstalled = installedPlugins.Any(p => p.PluginName == onlinePlugin.PluginName);
            tableData.AddRow([
                onlinePlugin.PluginName,
                onlinePlugin.PluginDescription,
                onlinePlugin.PluginAuthor,
                onlinePlugin.LatestVersion,
                isInstalled ? installedPlugins.First(p => p.PluginName == onlinePlugin.PluginName).PluginVersion : "Not Installed"
            ]);
        }

        tableData.HasRoundBorders = false;
        tableData.DisplayLinesBetweenRows = true;
        tableData.PrintTable();
    }

    internal static async Task RefreshPlugins(bool quiet)
    {
        try
        {
            await LoadPlugins(quiet ? ["-q"] : null);
            
        }catch(Exception ex)
        {
            Application.CurrentApplication.Logger.LogException(ex, typeof(PluginMethods), false);
        } finally
        {
            await Application.CurrentApplication.InternalActionManager.Initialize();
        }

    }

    internal static async Task DisablePlugin(string pluginName)
    {
        await Application.CurrentApplication.PluginManager.SetEnabledStatus(pluginName, false);
    }

    internal static async Task EnablePlugin(string pluginName)
    {
        await Application.CurrentApplication.PluginManager.SetEnabledStatus(pluginName, true);
    }

    public static async Task DownloadPluginWithParallelDownloads(string pluginName)
    {
        OnlinePlugin? pluginData = await Application.CurrentApplication.PluginManager.GetPluginDataByName(pluginName);
        
        if (pluginData is null)
        {
            Console.WriteLine($"Plugin {pluginName} not found. Please check the spelling and try again.");
            return;
        }

        var result = await Application.CurrentApplication.PluginManager.GatherInstallDataForPlugin(pluginData);
         List<Tuple<string, string>> downloadList = result.Item1.Select(kvp => new Tuple<string, string>(kvp.Key, kvp.Value)).ToList();

        await ConsoleUtilities.ExecuteParallelDownload(FileDownloader.CreateDownloadTask, new HttpClient(), downloadList, "Downloading:");
        
        await Application.CurrentApplication.PluginManager.AppendPluginToDatabase(PluginInfo.FromOnlineInfo(pluginData, result.Item2));
    }
    

    internal static async Task<bool> LoadPlugins(string[] args)
    {
        var loader = new PluginLoader(Application.CurrentApplication.DiscordBotClient.Client);
        if (args != null && args.Contains("-q"))
        {
            await loader.LoadPlugins();
            return true;
        }
        
        loader.OnCommandLoaded += (command) =>
        {
            Application.CurrentApplication.Logger.Log($"Command {command.Command} loaded successfully", LogType.Info);
        };
        
        loader.OnEventLoaded += (eEvent) =>
        {
            Application.CurrentApplication.Logger.Log($"Event {eEvent.Name} loaded successfully",LogType.Info);
        };
        
        loader.OnActionLoaded += (action) =>
        {
            Application.CurrentApplication.Logger.Log($"Action {action.ActionName} loaded successfully", LogType.Info);
        };
        
        loader.OnSlashCommandLoaded += (slashCommand) =>
        {
            Application.CurrentApplication.Logger.Log($"Slash Command {slashCommand.Name} loaded successfully", LogType.Info);
        };

        await loader.LoadPlugins();
        return true;
    }


}
