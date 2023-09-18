using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using DiscordBot.Utilities;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;
using Spectre.Console;

namespace DiscordBot.Bot.Actions;

public class Plugin : ICommandAction
{
    private bool pluginsLoaded;
    public string ActionName => "plugin";
    public string Description => "Manages plugins. Use plugin help for more info.";
    public string Usage => "plugin [help|list|load|install|refresh]";
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

        var manager = new PluginsManager();

        switch (args[0])
        {
            case "refresh":
                await Program.internalActionManager.Refresh();
                break;
            case "list":

                var data = await manager.GetAvailablePlugins();
                var items = new List<string[]>
                {
                    new[] { "-", "-", "-", "-" },
                    new[] { "Name", "Description", "Type", "Version" },
                    new[] { "-", "-", "-", "-" }
                };

                foreach (var plugin in data) items.Add(new[] { plugin[0], plugin[1], plugin[2], plugin[3] });

                items.Add(new[] { "-", "-", "-", "-" });

                ConsoleUtilities.FormatAndAlignTable(items, TableFormat.DEFAULT);
                break;


            case "load":
                if (pluginsLoaded)
                    break;
                var loader = new PluginLoader(Config.DiscordBot.client);
                if (args.Length == 2 && args[1] == "-q")
                {
                    loader.LoadPlugins();
                    pluginsLoaded = true;
                    break;
                }

                var cc = Console.ForegroundColor;
                loader.onCMDLoad += (name, typeName, success, exception) =>
                {
                    if (name == null || name.Length < 2)
                        name = typeName;
                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[CMD] Successfully loaded command : " + name);
                    }

                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        if (exception is null)
                            Console.WriteLine("An error occured while loading: " + name);
                        else
                            Console.WriteLine("[CMD] Failed to load command : " + name + " because " +
                                              exception!.Message
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
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[EVENT] Successfully loaded event : " + name);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[EVENT] Failed to load event : " + name + " because " + exception!.Message);
                    }

                    Console.ForegroundColor = cc;
                };

                loader.onSLSHLoad += (name, typeName, success, exception) =>
                {
                    if (name == null || name.Length < 2)
                        name = typeName;

                    if (success)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("[SLASH] Successfully loaded command : " + name);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine("[SLASH] Failed to load command : " + name + " because " +
                                          exception!.Message
                        );
                    }

                    Console.ForegroundColor = cc;
                };

                loader.LoadPlugins();
                Console.ForegroundColor = cc;
                pluginsLoaded = true;
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

                await DownloadPlugin(manager, pluginName);
                break;
        }
    }

    private async Task RefreshPlugins()
    {
        Console.WriteLine("Reloading plugins list...");
        await Program.internalActionManager.Execute("plugin", "load");
        await Program.internalActionManager.Refresh();

        Console.WriteLine("Finished reloading plugins list");
    }


    public async Task DownloadPlugin(PluginsManager manager, string pluginName)
    {
        var pluginData = await manager.GetPluginLinkByName(pluginName);
        if (pluginData.Length == 0)
        {
            Console.WriteLine($"Plugin {pluginName} not found. Please check the spelling and try again.");
            return;
        }

        var pluginType = pluginData[0];
        var pluginLink = pluginData[1];
        var pluginRequirements = pluginData[2];
        
        
        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn()
            })
            .StartAsync(async ctx =>
            {
                var downloadTask = ctx.AddTask("Downloading plugin...");

                IProgress<float> progress = new Progress<float>(p => { downloadTask.Value = p; });

                await ServerCom.DownloadFileAsync(pluginLink, $"./Data/{pluginType}s/{pluginName}.dll", progress);
                
                downloadTask.Increment(100);
                
                ctx.Refresh();
            });

        if (pluginRequirements == string.Empty)
        {
            Console.WriteLine("Finished installing " + pluginName + " successfully");
            await RefreshPlugins();
            return;
        }

        List<string> requirementsUrLs = new();
        
        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn()
            })
            .StartAsync(async ctx =>
            {
                var gatherInformationTask = ctx.AddTask("Gathering info...");
                gatherInformationTask.IsIndeterminate = true;
                requirementsUrLs = await ServerCom.ReadTextFromURL(pluginRequirements);
                await Task.Delay(2000);
                gatherInformationTask.Increment(100);
            });

        await AnsiConsole.Progress()
            .Columns(new ProgressColumn[]
            {
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn()
            })
            .StartAsync(async ctx =>
            {
                List<Tuple<ProgressTask, IProgress<float>, Task>> downloadTasks = new();

                foreach (var info in requirementsUrLs)
                {
                    if (info.Length < 2) continue;
                    string[] data = info.Split(',');
                    string url = data[0];
                    string fileName = data[1];
                    
                    var task = ctx.AddTask($"Downloading {fileName}...");
                    IProgress<float> progress = new Progress<float>(p =>
                    {
                        task.Value = p;
                    });
                    
                    var downloadTask = ServerCom.DownloadFileAsync(url, $"./{fileName}", progress);
                    downloadTasks.Add(new Tuple<ProgressTask, IProgress<float>, Task>(task, progress, downloadTask));
                }
                
                foreach (var task in downloadTasks)
                {
                    await task.Item3;
                }
                
            });

        await RefreshPlugins();
    }
}