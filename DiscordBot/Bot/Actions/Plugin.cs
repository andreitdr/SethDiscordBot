using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;

namespace DiscordBot.Bot.Actions;

public class Plugin : ICommandAction
{
    public string ActionName => "plugin";
    public string Description => "Manages plugins. Use plugin help for more info.";
    public string Usage => "plugin [help|list|load|install]";
    public InternalActionRunType RunType => InternalActionRunType.ON_CALL;

    private bool pluginsLoaded = false;

    public async Task Execute(string[] args)
    {
        if (args is null || args.Length == 0 || args[0] == "help")
        {
            Console.WriteLine("Usage : plugin [help|list|load|install|remove|update]");
            Console.WriteLine("help : Displays this message");
            Console.WriteLine("list : Lists all plugins");
            Console.WriteLine("load : Loads all plugins");
            Console.WriteLine("install : Installs a plugin");

            return;
        }

        switch (args[0])
        {
            case "list":
                var manager = new PluginManager.Online.PluginsManager(Program.URLs["PluginList"], Program.URLs["PluginVersions"]);

                var data = await manager.GetAvailablePlugins();
                var items = new List<string[]>
                {
                    new[] { "-", "-", "-", "-" },
                    new[] { "Name", "Type", "Description", "Required" },
                    new[] { "-", "-", "-", "-" }
                };

                foreach (var plugin in data)
                {
                    items.Add(new[] { plugin[0], plugin[1], plugin[2], plugin[3] });
                }

                items.Add(new[] { "-", "-", "-", "-" });

                Utilities.Utilities.FormatAndAlignTable(items, Utilities.TableFormat.DEFAULT);
                break;
            
            
            case "load":
                if (pluginsLoaded)
                    break;
                var loader = new PluginLoader(PluginManager.Config.DiscordBot.client);
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
                                              exception!.Message);
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
                                          exception!.Message);
                    }

                    Console.ForegroundColor = cc;
                };

                loader.LoadPlugins();
                Console.ForegroundColor = cc;
                pluginsLoaded = true;
                break;
            
            case "install":
                string pluginName = string.Join(' ', args, 1, args.Length-1);
                if (string.IsNullOrEmpty(pluginName)|| pluginName.Length < 2)
                {
                    Console.WriteLine("Please specify a plugin name");
                    break;
                }
                
                var pluginManager = new PluginManager.Online.PluginsManager(Program.URLs["PluginList"], Program.URLs["PluginVersions"]);
                string[] pluginData = await pluginManager.GetPluginLinkByName(pluginName);
                if (pluginData == null || pluginData.Length == 0)
                {
                    Console.WriteLine("Plugin not found");
                    break;
                }

                string pluginType = pluginData[0];
                string pluginLink = pluginData[1];
                string pluginRequirements = pluginData[2];
                
                
                Console.WriteLine("Downloading plugin...");
                //download plugin progress bar for linux and windows terminals
                Utilities.Utilities.Spinner spinner = new Utilities.Utilities.Spinner();
                spinner.Start();
                await ServerCom.DownloadFileAsync(pluginLink, $"./Data/{pluginType}s/{pluginName}.dll", null);
                spinner.Stop();
                Console.WriteLine();

                if (pluginRequirements == string.Empty)
                {
                    Console.WriteLine("Plugin installed successfully");
                    break;
                }
                
                Console.WriteLine("Downloading plugin requirements...");
                List<string> requirementsURLs = await ServerCom.ReadTextFromURL(pluginRequirements);

                foreach (var requirement in requirementsURLs)
                {
                    if(requirement.Length < 2)
                        continue;
                    string[] reqdata = requirement.Split(',');
                    string url = reqdata[0];
                    string filename = reqdata[1];
                    
                    Console.WriteLine($"Downloading {filename}... ");
                    spinner.Start();

                    await ServerCom.DownloadFileAsync(url, $"./{filename}.dll", null);
                    spinner.Stop();
                    await Task.Delay(1000);
                    Console.WriteLine("Downloaded " + filename + " successfully");
                }
                
                Console.WriteLine("Finished installing " + pluginName + " successfully");

                break;
        }
    }
}
