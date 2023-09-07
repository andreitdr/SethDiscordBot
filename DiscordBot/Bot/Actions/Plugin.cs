using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBot.Utilities;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;

namespace DiscordBot.Bot.Actions;

public class Plugin : ICommandAction
{
    private bool                  pluginsLoaded;
    public  string                ActionName  => "plugin";
    public  string                Description => "Manages plugins. Use plugin help for more info.";
    public  string                Usage       => "plugin [help|list|load|install|refresh]";
    public  InternalActionRunType RunType     => InternalActionRunType.ON_CALL;

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

        switch ( args[0] )
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
                
                var cc     = Console.ForegroundColor;
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
                pluginsLoaded           = true;
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
                
                var pluginData = await manager.GetPluginLinkByName(pluginName);
                if (pluginData == null || pluginData.Length == 0)
                {
                    Console.WriteLine($"Plugin {pluginName} not found. Please check the spelling and try again.");
                    break;
                }

                var pluginType         = pluginData[0];
                var pluginLink         = pluginData[1];
                var pluginRequirements = pluginData[2];


                Console.WriteLine("Downloading plugin...");
                //download plugin progress bar for linux and windows terminals
                var spinner = new ConsoleUtilities.Spinner();
                spinner.Start();
                IProgress<float> progress = new Progress<float>(p => { spinner.Message = $"Downloading {pluginName}... {Math.Round(p, 2)}%  "; });
                await ServerCom.DownloadFileAsync(pluginLink, $"./Data/{pluginType}s/{pluginName}.dll", progress);
                spinner.Stop();
                Console.WriteLine();

                if (pluginRequirements == string.Empty)
                {
                    Console.WriteLine("Finished installing " + pluginName + " successfully");
                    Console.WriteLine("Reloading plugins list...");
                    await Program.internalActionManager.Execute("plugin", "load");
                    await Program.internalActionManager.Refresh();
                
                    Console.WriteLine("Finished reloading plugins list");
                    break;
                }

                Console.WriteLine("Downloading plugin requirements...");
                var requirementsURLs = await ServerCom.ReadTextFromURL(pluginRequirements);

                foreach (var requirement in requirementsURLs)
                {
                    if (requirement.Length < 2)
                        continue;
                    var reqdata  = requirement.Split(',');
                    var url      = reqdata[0];
                    var filename = reqdata[1];

                    Console.WriteLine($"Downloading {filename}... ");
                    progress = new Progress<float>(p => { spinner.Message = $"Downloading {filename}... {Math.Round(p, 2)}%  "; });
                    spinner.Start();
                    await ServerCom.DownloadFileAsync(url, $"./{filename}", progress);
                    spinner.Stop();
                    progress.Report(1);
                    await Task.Delay(1000);
                    Console.WriteLine("Downloaded " + filename + " successfully");
                }

                Console.WriteLine("Finished installing " + pluginName + " successfully");
                
                Console.WriteLine("Reloading plugins list...");
                await Program.internalActionManager.Execute("plugin", "load", "-q");
                await Program.internalActionManager.Refresh();
                
                Console.WriteLine("Finished reloading plugins list");
                break;
        }
    }
}
