using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Utilities;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;
using PluginManager.Plugin;
using Spectre.Console;

namespace DiscordBot.Bot.Actions.Extra;

internal static class PluginMethods
{
    internal static async Task List(PluginsManager manager)
    {
        var data = await ConsoleUtilities.ExecuteWithProgressBar(manager.GetPluginsList(), "Loading plugins...");

        TableData tableData = new(new List<string> { "Name", "Description", "Version", "Has Dependencies" });
        foreach (var plugin in data) tableData.AddRow([plugin.Name, plugin.Description, plugin.Version.ToString(), plugin.HasDependencies ? "Yes" : "No"]);

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
                                 new TaskDescriptionColumn(),
                                 new ProgressBarColumn(),
                                 new PercentageColumn()
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
            Console.WriteLine("Finished installing " + pluginName + " successfully");
            await RefreshPlugins(false);
            return;
        }
        
        List<Tuple<ProgressTask, IProgress<float>, string, string>> downloadTasks = new();
        await AnsiConsole.Progress()
                         .Columns(new ProgressColumn[]
                             {
                                 new TaskDescriptionColumn(),
                                 new ProgressBarColumn(),
                                 new PercentageColumn()
                             }
                         )
                         .StartAsync(async ctx =>
                             {


                                 foreach (OnlineDependencyInfo dependency in pluginData.Dependencies)
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

                                 if (!int.TryParse(Config.AppSettings["MaxParallelDownloads"], out int maxParallelDownloads))
                                 {
                                     maxParallelDownloads = 5;
                                     Config.AppSettings.Add("MaxParallelDownloads", "5");
                                     await Config.AppSettings.SaveToFile();
                                 }

                                 var options = new ParallelOptions()
                                 {
                                     MaxDegreeOfParallelism = maxParallelDownloads,
                                     TaskScheduler          = TaskScheduler.Default
                                 };

                                 await Parallel.ForEachAsync(downloadTasks, options, async (tuple, token) =>
                                     {
                                         tuple.Item1.IsIndeterminate = false;
                                         await ServerCom.DownloadFileAsync(tuple.Item3, $"./{tuple.Item4}", tuple.Item2);
                                     }
                                 );



                             }
                         );

        
        await RefreshPlugins(false);
    }

    internal static async Task<bool> LoadPlugins(string[] args)
    {
        var loader = new PluginLoader(Config.DiscordBot.client);
        if (args.Length == 2 && args[1] == "-q")
        {
            loader.LoadPlugins();
            return true;
        }

        var cc = Console.ForegroundColor;
        loader.onCMDLoad += (name, typeName, success, exception) =>
        {
            if (name == null || name.Length < 2)
                name = typeName;
            if (success)
            {
                Config.Logger.Log("Successfully loaded command : " + name, source: typeof(ICommandAction),
                    type: LogType.INFO
                );
            }

            else
            {
                Config.Logger.Log("Failed to load command : " + name + " because " + exception?.Message,
                    source: typeof(ICommandAction), type: LogType.ERROR
                );
            }

            Console.ForegroundColor = cc;
        };
        loader.onEVELoad += (name, typeName, success, exception) =>
        {
            if (name == null || name.Length < 2)
                name = typeName;

            if (success)
            {
                Config.Logger.Log("Successfully loaded event : " + name, source: typeof(ICommandAction),
                    type: LogType.INFO
                );
            }
            else
            {
                Config.Logger.Log("Failed to load event : " + name + " because " + exception?.Message,
                    source: typeof(ICommandAction), type: LogType.ERROR
                );
            }

            Console.ForegroundColor = cc;
        };

        loader.onSLSHLoad += (name, typeName, success, exception) =>
        {
            if (name == null || name.Length < 2)
                name = typeName;

            if (success)
            {
                Config.Logger.Log("Successfully loaded slash command : " + name, source: typeof(ICommandAction),
                    type: LogType.INFO
                );
            }
            else
            {
                Config.Logger.Log("Failed to load slash command : " + name + " because " + exception?.Message,
                    source: typeof(ICommandAction), type: LogType.ERROR
                );
            }

            Console.ForegroundColor = cc;
        };

        loader.LoadPlugins();
        Console.ForegroundColor = cc;
        return true;
    }


}
