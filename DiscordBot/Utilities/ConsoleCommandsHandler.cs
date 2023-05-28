using System.Threading;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;

using Discord.WebSocket;

using PluginManager;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;

using OperatingSystem = PluginManager.Others.OperatingSystem;

namespace DiscordBot.Utilities;

public class ConsoleCommandsHandler
{

    public static ConsoleCommandsHandler handler;
    private readonly PluginsManager manager;

    private readonly List<ConsoleCommand> commandList = new();


    private bool isDownloading;
    private bool pluginsLoaded;
    private DiscordSocketClient client;

    public ConsoleCommandsHandler(DiscordSocketClient client)
    {
        this.client = client;
        
        manager = new PluginsManager(Program.URLs["PluginList"], Program.URLs["PluginVersions"]);
        InitializeBasicCommands();
        Console.WriteLine("Done");

        if (handler == null)
            handler = this;
        else
            throw new Exception("ConsoleCommandsHandler already initialized");
    }

    private void InitializeBasicCommands()
    {
        commandList.Clear();

        AddCommand("help", "Show help", "help <command>", args =>
            {
                if (args.Length <= 1)
                {
                    Console.WriteLine("Available commands:");
                    var items = new List<string[]>
                    {
                        new[] { "-", "-", "-" },
                        new[] { "Command", "Description", "Usage" },
                        new[] { " ", " ", "Argument type: <optional> [required]" },
                        new[] { "-", "-", "-" }
                    };

                    foreach (var command in commandList)
                    {
                        if (!command.CommandName.StartsWith("_"))
                            items.Add(new[] { command.CommandName, command.Description, command.Usage });
                    }

                    items.Add(new[] { "-", "-", "-" });
                    Utilities.FormatAndAlignTable(items, TableFormat.DEFAULT);
                }
                else
                {
                    foreach (var command in commandList)
                        if (command.CommandName == args[1])
                        {
                            Console.WriteLine("Command description: " + command.Description);
                            Console.WriteLine("Command execution format:" + command.Usage);
                            return;
                        }

                    Console.WriteLine("Command not found");
                }
            }
        );

        AddCommand("lp", "Load plugins", () =>
            {
                if (pluginsLoaded)
                    return;
                var loader = new PluginLoader(client!);
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
                            Console.WriteLine("[CMD] Failed to load command : " + name + " because " + exception!.Message);
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
                        Console.WriteLine("[SLASH] Failed to load command : " + name + " because " + exception!.Message);
                    }

                    Console.ForegroundColor = cc;
                };

                loader.LoadPlugins();
                Console.ForegroundColor = cc;
                pluginsLoaded = true;
            }
        );

        AddCommand("listplugs", "list available plugins", async () => {
            if(manager == null)
            {
                Console.WriteLine("Plugin manager is null");
                return;
            }
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
            Utilities.FormatAndAlignTable(items, TableFormat.DEFAULT);
        });

        AddCommand("dwplug", "download plugin", "dwplug [name]", async args =>
            {
                isDownloading = true;
                Utilities.Spinner spinner = new Utilities.Spinner();
                if (args.Length == 1)
                {
                    isDownloading = false;
                    Console.WriteLine("Please specify plugin name");
                    return;
                }

                var name = string.Join(' ', args, 1, args.Length - 1);
                // info[0] = plugin type
                // info[1] = plugin link
                // info[2] = if others are required, or string.Empty if none
                var info = await manager.GetPluginLinkByName(name);
                if (info[1] == null) // link is null
                {
                    if (name == "")
                    {
                        isDownloading = false;
                        Utilities.WriteColorText("Name is invalid");
                        return;
                    }

                    isDownloading = false;
                    Utilities.WriteColorText($"Failed to find plugin &b{name} &c!" +
                                                     " Use &glistplugs &ccommand to display all available plugins !");
                    return;
                }

                spinner.Start();

                string path;
                if (info[0] == "Plugin")
                    path = "./Data/Plugins/" + name + ".dll";
                else
                    path = $"./{info[1].Split('/')[info[1].Split('/').Length - 1]}";

                
                Console.WriteLine($"Downloading plugin {name} from {info[1]} to {path}");
                await ServerCom.DownloadFileAsync(info[1], path, null);

                Console.WriteLine("\n");

                // check requirements if any

                if (info.Length == 3 && info[2] != string.Empty && info[2] != null)
                {
                    Console.WriteLine($"Downloading requirements for plugin : {name}");

                    var lines = await ServerCom.ReadTextFromURL(info[2]);

                    foreach (var line in lines)
                    {
                        if (!(line.Length > 0 && line.Contains(",")))
                            continue;
                        var split = line.Split(',');
                        Console.WriteLine($"\nDownloading item: {split[1]}");
                        if (File.Exists("./" + split[1])) File.Delete("./" + split[1]);
                        await ServerCom.DownloadFileAsync(split[0], "./" + split[1], null);
                            
                        Console.WriteLine("Item " + split[1] + " downloaded !");

                        Console.WriteLine();
                        if (split[0].EndsWith(".pak"))
                        {
                            File.Move("./" + split[1], "./Data/PAKS/" + split[1], true);
                        }
                        else if (split[0].EndsWith(".zip") || split[0].EndsWith(".pkg"))
                        {
                            Console.WriteLine($"Extracting {split[1]} ...");
                            await ArchiveManager.ExtractArchive("./" + split[1], "./", null,
                                                           UnzipProgressType.PERCENTAGE_FROM_TOTAL_SIZE);
                            Console.WriteLine("\n");
                            File.Delete("./" + split[1]);
                        }
                    }

                    Console.WriteLine();
                }
                spinner.Stop();

                var ver = await manager.GetVersionOfPackageFromWeb(name);
                if (ver is null) throw new Exception("Incorrect version");
                    Config.Plugins[name] = ver.ToShortString();

                isDownloading = false;
                

                Config.Logger.Log("Plugin installed !", this, LogLevel.INFO);

                //await ExecuteCommad("localload " + name);
            }
        );


        AddCommand("value", "read value from VariableStack", "value [key]", args =>
            {
                if (args.Length != 2)
                    return;
                if (!Config.Data.ContainsKey(args[1]))
                    return;

                var data = Config.Data[args[1]];
                Console.WriteLine($"{args[1]} => {data}");
            }
        );

        AddCommand("add", "add variable to the system variables", "add [key] [value]", args =>
            {
                if (args.Length < 4)
                    return;
                var key = args[1];
                var value = args[2];
                var isReadOnly = args[3].Equals("true", StringComparison.CurrentCultureIgnoreCase);

                try
                {
                    Config.Data[key] = value;
                    Console.WriteLine($"Updated config file with the following command: {args[1]} => {value}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                }
            }
        );

        AddCommand("remv", "remove variable from system variables", "remv [key]", args =>
            {
                if (args.Length < 2)
                    return;
                Config.Data.Remove(args[1]);
            }
        );

        AddCommand("sd", "Shuts down the discord bot", async () =>
            {
                if (client is null)
                    return;

                await Functions.SaveToJsonFile(Functions.dataFolder + "config.json", Config.Data);
                await Functions.SaveToJsonFile(Functions.dataFolder + "Plugins.json", Config.Plugins);
                await client.StopAsync();
                await client.DisposeAsync();

                Config.Logger.SaveToFile();
                await Task.Delay(1000);
                Environment.Exit(0);
            }
        );

        AddCommand("import", "Load an external command", "import [pluginName]", async args =>
        {
            if (args.Length <= 1) return;
            try
            {
                var pName = string.Join(' ', args, 1, args.Length - 1);
                using (var client = new HttpClient())
                {
                    var url = (await manager.GetPluginLinkByName(pName))[1];
                    if (url is null) throw new Exception($"Invalid plugin name {pName}.");
                    var s = await client.GetStreamAsync(url);
                    var str = new MemoryStream();
                    await s.CopyToAsync(str);
                    var asmb = Assembly.Load(str.ToArray());

                    await PluginLoader.LoadPluginFromAssembly(asmb, this.client);
                }


            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        });

        AddCommand("localload", "Load a local command", "local [pluginName]", async args =>
        {
            if (args.Length <= 1) return;
            try
            {
                var pName = string.Join(' ', args, 1, args.Length - 1);
                var asmb = Assembly.LoadFile(Path.GetFullPath("./Data/Plugins/" + pName + ".dll"));

                await PluginLoader.LoadPluginFromAssembly(asmb, this.client);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Config.Logger.Log(ex.Message, this, LogLevel.ERROR);
            }
        });


        commandList.Sort((x, y) => x.CommandName.CompareTo(y.CommandName));
    }

    public void AddCommand(string command, string description, string usage, Action<string[]> action)
    {
        Console.WriteLine($"Adding command {command} ...");
        commandList.Add(new ConsoleCommand
        { CommandName = command, Description = description, Action = action, Usage = usage });
        Console.ForegroundColor = ConsoleColor.White;
        Utilities.WriteColorText($"Command &r{command} &cadded to the list of commands");
    }

    public void AddCommand(string command, string description, Action action)
    {
        AddCommand(command, description, command, args => action());
    }

    public void RemoveCommand(string command)
    {
        commandList.RemoveAll(x => x.CommandName == command);
    }

    public bool CommandExists(string command)
    {
        return GetCommand(command) is not null;
    }

    public ConsoleCommand? GetCommand(string command)
    {
        return commandList.FirstOrDefault(t => t.CommandName == command);
    }

    public bool HandleCommand(string command, bool removeCommandExecution = true)
    {
        Console.ForegroundColor = ConsoleColor.White;
        var args = command.Split(' ');
        foreach (var item in commandList.ToList())
            if (item.CommandName == args[0])
            {
                if (args[0].StartsWith("_"))
                    throw new Exception("This command is reserved for internal worker and can not be executed manually !");

                    if (removeCommandExecution)
                    {
                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        for (var i = 0; i < command.Length + 30; i++)
                            Console.Write(" ");
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }

                Console.WriteLine();
                item.Action(args);

                return true;
            }

        return false;
    }
}