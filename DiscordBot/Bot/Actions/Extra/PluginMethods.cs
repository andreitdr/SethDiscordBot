using System;
using System.Collections.Generic;
using System.Linq;
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
        Console.WriteLine($"Fetching plugin list from branch {Application.CurrentApplication.PluginManager.Branch} ...");

        var data = await ConsoleUtilities.ExecuteWithProgressBar(Application.CurrentApplication.PluginManager.GetPluginsList(), "Reading remote database");

        TableData tableData = new(["Name", "Description", "Version", "Installed", "Dependencies", "Enabled"]);

        var installedPlugins = await ConsoleUtilities.ExecuteWithProgressBar(Application.CurrentApplication.PluginManager.GetInstalledPlugins(), "Reading local database ");

        foreach (var plugin in data)
        {
            bool       isInstalled     = installedPlugins.Any(p => p.PluginName == plugin.Name);
            
            if (!plugin.HasFileDependencies)
            {
                tableData.AddRow([plugin.Name, plugin.Description,
                    plugin.Version.ToString(), isInstalled ? "[green]Yes[/]" : "[red]No[/]", "None",
                    isInstalled ? installedPlugins.First(p=>p.PluginName == plugin.Name).IsEnabled ? "[green]Enabled[/]" : "[red]Disabled[/]" : "[yellow]NOT INSTALLED[/]"]);
                continue;
            }

            TableData dependenciesTable;
            

            if (isInstalled)
            {
                dependenciesTable = new(["Name", "Location", "Is Executable"]);
                foreach (var dep in plugin.Dependencies)
                {
                    dependenciesTable.AddRow([dep.DependencyName, dep.DownloadLocation, dep.IsExecutable ? "Yes" : "No"]);
                }

            } 
            else
            {
                dependenciesTable = new(["Name", "Is Executable"]);
                foreach (var dep in plugin.Dependencies)
                {
                    dependenciesTable.AddRow([dep.DependencyName, dep.IsExecutable ? "Yes" : "No"]);
                }
            }

            dependenciesTable.DisplayLinesBetweenRows = true;

            Table spectreTable = dependenciesTable.AsTable();

            tableData.AddRow([plugin.Name, plugin.Description, plugin.Version.ToString(), isInstalled ? "[green]Yes[/]" : "[red]No[/]", spectreTable,
                isInstalled ? installedPlugins.First(p=>p.PluginName == plugin.Name).IsEnabled ? "Enabled" : "[red]Disabled[/]" : "[yellow]NOT INSTALLED[/]"]);
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
            Application.Logger.LogException(ex, typeof(PluginMethods), false);
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

    internal static async Task DownloadPlugin(string pluginName)
    {
        var pluginData = await Application.CurrentApplication.PluginManager.GetPluginDataByName(pluginName);
        if (pluginData is null)
        {
            Console.WriteLine($"Plugin {pluginName} not found. Please check the spelling and try again.");
            return;
        }

        // rename the plugin to the name of the plugin
        pluginName = pluginData.Name;

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

                                 string baseFolder = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder");

                                 await ServerCom.DownloadFileAsync(pluginLink, $"{baseFolder}/{pluginData.Name}.dll", progress);

                                 downloadTask.Increment(100);

                                 ctx.Refresh();
                             }
                         );

        if (!pluginData.HasFileDependencies)
        {
            if (pluginData.HasScriptDependencies)
            {
                Console.WriteLine("Executing post install scripts ...");
                await Application.CurrentApplication.PluginManager.ExecutePluginInstallScripts(pluginData.ScriptDependencies);
            }

            PluginInfo pluginInfo = new(pluginName, pluginData.Version, []);
            Console.WriteLine("Finished installing " + pluginName + " successfully");
            await Application.CurrentApplication.PluginManager.AppendPluginToDatabase(pluginInfo);
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
                                     var task = ctx.AddTask($"Downloading {dependency.DownloadLocation} -> Executable: {dependency.IsExecutable}: ");
                                     IProgress<float> progress = new Progress<float>(p =>
                                         {
                                             task.Value = p;
                                         }
                                     );

                                     task.IsIndeterminate = true;
                                     downloadTasks.Add(new Tuple<ProgressTask, IProgress<float>, string, string>(task, progress, dependency.DownloadLink, dependency.DownloadLocation));
                                 }
                                 
                                 int maxParallelDownloads = Application.CurrentApplication.ApplicationEnvironmentVariables.Get("MaxParallelDownloads", 5);
                                 
                                 var options = new ParallelOptions()
                                 {
                                     MaxDegreeOfParallelism = maxParallelDownloads,
                                     TaskScheduler = TaskScheduler.Default
                                 };

                                 await Parallel.ForEachAsync(downloadTasks, options, async (tuple, token) =>
                                     {
                                         tuple.Item1.IsIndeterminate = false;
                                         string downloadLocation = Application.CurrentApplication.PluginManager.GenerateDependencyRelativePath(pluginName, tuple.Item4);
                                         await ServerCom.DownloadFileAsync(tuple.Item3, downloadLocation, tuple.Item2);
                                     }
                                 );



                             }
                         );

        
        if(pluginData.HasScriptDependencies)
        {
            Console.WriteLine("Executing post install scripts ...");
            await Application.CurrentApplication.PluginManager.ExecutePluginInstallScripts(pluginData.ScriptDependencies);
        }

        await Application.CurrentApplication.PluginManager.AppendPluginToDatabase(PluginInfo.FromOnlineInfo(pluginData));
        await RefreshPlugins(false);
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
            Application.Logger.Log($"Command {command.Command} loaded successfully", LogType.Info);
        };
        
        loader.OnEventLoaded += (eEvent) =>
        {
            Application.Logger.Log($"Event {eEvent.Name} loaded successfully",LogType.Info);
        };
        
        loader.OnActionLoaded += (action) =>
        {
            Application.Logger.Log($"Action {action.ActionName} loaded successfully", LogType.Info);
        };
        
        loader.OnSlashCommandLoaded += (slashCommand) =>
        {
            Application.Logger.Log($"Slash Command {slashCommand.Name} loaded successfully", LogType.Info);
        };

        await loader.LoadPlugins();
        return true;
    }


}
