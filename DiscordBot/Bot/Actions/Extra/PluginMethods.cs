using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DiscordBot.Utilities;

using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;

using Spectre.Console;

namespace DiscordBot.Bot.Actions.Extra;

internal static class PluginMethods
{
    internal static async Task List(PluginsManager manager)
    {
        var data = await ConsoleUtilities.ExecuteWithProgressBar(manager.GetPluginsList(), "Loading plugins...");

        TableData tableData = new(new List<string>
            {
                "Name",
                "Description",
                "Version",
                "Is Installed"
            }
        );

        var installedPlugins = await manager.GetInstalledPlugins();

        foreach (var plugin in data)
        {
            bool isInstalled = installedPlugins.Any(p => p.PluginName == plugin.Name);
            tableData.AddRow([plugin.Name, plugin.Description, plugin.Version.ToString(), isInstalled ? "Yes" : "No"]);
        }

        tableData.HasRoundBorders = false;
        tableData.PrintAsTable();
    }

    internal static async Task RefreshPlugins(bool quiet)
    {
        await Program.internalActionManager.Execute("plugin", "load", quiet ? "-q" : string.Empty);
        await Program.internalActionManager.Refresh();
    }

    internal static async Task DownloadPlugin(PluginsManager manager, string pluginName)
    {
        var pluginData = await manager.GetPluginDataByName(pluginName);
        if (pluginData is null)
        {
            Console.WriteLine($"Plugin {pluginName} not found. Please check the spelling and try again.");
            return;
        }

        var pluginLink = pluginData.DownLoadLink;


        await AnsiConsole.Progress()
                         .Columns(new ProgressColumn[]
                             {
                                 new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn()
                             }
                         )
                         .StartAsync(async ctx =>
                             {
                                 var downloadTask = ctx.AddTask("Downloading plugin...");

                                 IProgress<float> progress = new Progress<float>(p => { downloadTask.Value = p; });

                                 await ServerCom.DownloadFileAsync(pluginLink, $"{Config.AppSettings["PluginFolder"]}/{pluginName}.dll", progress);

                                 downloadTask.Increment(100);

                                 ctx.Refresh();
                             }
                         );

        if (!pluginData.HasDependencies)
        {
            await manager.AppendPluginToDatabase(new PluginManager.Plugin.PluginInfo(pluginName, pluginData.Version, []));
            Console.WriteLine("Finished installing " + pluginName + " successfully");
            await RefreshPlugins(false);
            return;
        }

        List<Tuple<ProgressTask, IProgress<float>, string, string>> downloadTasks = new();
        await AnsiConsole.Progress()
                         .Columns(new ProgressColumn[]
                             {
                                 new TaskDescriptionColumn(), new ProgressBarColumn(), new PercentageColumn()
                             }
                         )
                         .StartAsync(async ctx =>
                             {


                                 foreach (var dependency in pluginData.Dependencies)
                                 {
                                     var task = ctx.AddTask($"Downloading {dependency.DownloadLocation}: ");
                                     IProgress<float> progress = new Progress<float>(p =>
                                         {
                                             task.Value = p;
                                         }
                                     );

                                     task.IsIndeterminate = true;
                                     downloadTasks.Add(new Tuple<ProgressTask, IProgress<float>, string, string>(task, progress, dependency.DownloadLink, dependency.DownloadLocation));
                                 }

                                 int maxParallelDownloads = 5;

                                 if (Config.AppSettings.ContainsKey("MaxParallelDownloads"))
                                     maxParallelDownloads = int.Parse(Config.AppSettings["MaxParallelDownloads"]);

                                 var options = new ParallelOptions()
                                 {
                                     MaxDegreeOfParallelism = maxParallelDownloads,
                                     TaskScheduler = TaskScheduler.Default
                                 };

                                 await Parallel.ForEachAsync(downloadTasks, options, async (tuple, token) =>
                                     {
                                         tuple.Item1.IsIndeterminate = false;
                                         await ServerCom.DownloadFileAsync(tuple.Item3, $"./{tuple.Item4}", tuple.Item2);
                                     }
                                 );



                             }
                         );

        await manager.AppendPluginToDatabase(new PluginManager.Plugin.PluginInfo(pluginName, pluginData.Version, pluginData.Dependencies.Select(sep => sep.DownloadLocation).ToList()));
        await RefreshPlugins(false);
    }

    internal static async Task<bool> LoadPlugins(string[] args)
    {
        var loader = new PluginLoader(Config.DiscordBot.Client);
        if (args.Length == 2 && args[1] == "-q")
        {
            await loader.LoadPlugins();
            return true;
        }

        var cc = Console.ForegroundColor;
        loader.OnCommandLoaded += (data) =>
        {
            if (data.IsSuccess)
            {
                Config.Logger.Log("Successfully loaded command : " + data.PluginName, typeof(ICommandAction),
                    LogType.INFO
                );
            }

            else
            {
                Config.Logger.Log("Failed to load command : " + data.PluginName + " because " + data.ErrorMessage,
                    typeof(ICommandAction), LogType.ERROR
                );
            }

            Console.ForegroundColor = cc;
        };
        loader.OnEventLoaded += (data) =>
        {
            if (data.IsSuccess)
            {
                Config.Logger.Log("Successfully loaded event : " + data.PluginName, typeof(ICommandAction),
                    LogType.INFO
                );
            }
            else
            {
                Config.Logger.Log("Failed to load event : " + data.PluginName + " because " + data.ErrorMessage,
                    typeof(ICommandAction), LogType.ERROR
                );
            }

            Console.ForegroundColor = cc;
        };

        loader.OnSlashCommandLoaded += (data) =>
        {
            if (data.IsSuccess)
            {
                Config.Logger.Log("Successfully loaded slash command : " + data.PluginName, typeof(ICommandAction),
                    LogType.INFO
                );
            }
            else
            {
                Config.Logger.Log("Failed to load slash command : " + data.PluginName + " because " + data.ErrorMessage,
                    typeof(ICommandAction), LogType.ERROR
                );
            }

            Console.ForegroundColor = cc;
        };

        await loader.LoadPlugins();
        Console.ForegroundColor = cc;
        return true;
    }


}
