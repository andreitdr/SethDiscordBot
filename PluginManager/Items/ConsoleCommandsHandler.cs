using Discord.WebSocket;
using PluginManager.Loaders;
using PluginManager.Online;
using PluginManager.Others;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Reflection.Metadata.Ecma335;

namespace PluginManager.Items
{
    public class ConsoleCommandsHandler
    {
        private static PluginsManager manager = new("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Plugins");

        public static List<Tuple<string, string, Action<string[]>>>? commandList = new List<Tuple<string, string, Action<string[]>>>();
        private       DiscordSocketClient                            client;

        public ConsoleCommandsHandler(DiscordSocketClient client)
        {
            this.client = client;
            InitializeBasicCommands();
            Console.WriteLine("Initalized console command handeler !");
        }

        private void InitializeBasicCommands()
        {

            bool pluginsLoaded = false;
            commandList.Clear();

            AddCommand("help", "Show help", (args) =>
            {
                if (args.Length <= 1)
                {
                    Console.WriteLine("Available commands:");
                    foreach (var command in commandList)
                    {
                        Console.WriteLine("\t" + command.Item1 + " - " + command.Item2);
                    }
                }
                else
                {
                    foreach (var command in commandList)
                    {
                        if (command.Item1 == args[1])
                        {
                            Console.WriteLine(command.Item2);
                            return;
                        }
                    }
                    Console.WriteLine("Command not found");
                }
            });

            AddCommand("lp", "Load plugins", () =>
            {
                if (pluginsLoaded) return;
                var loader = new PluginLoader(client);
                loader.onCMDLoad += (name, typeName, success, exception) =>
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (name == null || name.Length < 2)
                        name = typeName;
                    if (success)
                        Console.WriteLine("[CMD] Successfully loaded command : " + name);
                    else
                        Console.WriteLine("[CMD] Failed to load command : " + name + " because " + exception.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                };
                loader.onEVELoad += (name, typeName, success, exception) =>
                {
                    if (name == null || name.Length < 2)
                        name = typeName;
                    Console.ForegroundColor = ConsoleColor.Green;
                    if (success)
                        Console.WriteLine("[EVENT] Successfully loaded event : " + name);
                    else
                        Console.WriteLine("[EVENT] Failed to load event : " + name + " because " + exception.Message);
                    Console.ForegroundColor = ConsoleColor.Red;
                };
                loader.LoadPlugins();
                pluginsLoaded = true;
            });

            AddCommand("listplugs", "list available plugins", async () =>
            {
                await manager.ListAvailablePlugins();
            });

            AddCommand("dwplug", "download plugin", async (args) =>
            {
                if (args.Length == 1)
                {
                    Console.WriteLine("Please specify plugin name");
                    return;
                }

                string name = args.MergeStrings(1);
                // info[0] = plugin type
                // info[1] = plugin link
                // info[2] = if others are required, or string.Empty if none
                string[] info = await manager.GetPluginLinkByName(name);
                if (info[1] == null) // link is null
                {
                    if (name == "")
                    {
                        Console_Utilities.WriteColorText($"Name is invalid");
                        return;
                    }
                    Console_Utilities.WriteColorText($"Failed to find plugin &b{name} &c!" +
                        $" Use &glistplugs &ccommand to display all available plugins !");
                    return;

                }
                string path;
                if (info[0] == "Command" || info[0] == "Event")
                    path = "./Data/Plugins/" + info[0] + "s/" + name + ".dll";
                else path = $"./{info[1].Split('/')[info[1].Split('/').Length - 1]}";
                await ServerCom.DownloadFileAsync(info[1], path);
                Console.WriteLine("\n");

                // check requirements if any

                if (info.Length == 3 && info[2] != string.Empty && info[2] != null)
                {
                    Console.WriteLine($"Downloading requirements for plugin : {name}");

                    List<string> lines = await ServerCom.ReadTextFromFile(info[2]);

                    foreach (var line in lines)
                    {
                        string[] split = line.Split(',');
                        Console.WriteLine($"\nDownloading item: {split[1]}");
                        await ServerCom.DownloadFileAsync(split[0], "./" + split[1]);
                        Console.WriteLine();

                        if (split[0].EndsWith(".zip"))
                        {

                            Console.WriteLine($"Extracting {split[1]}");
                            double proc = 0d;
                            bool isExtracting = true;
                            Console_Utilities.ProgressBar bar = new Console_Utilities.ProgressBar(100, "");

                            IProgress<float> extractProgress = new Progress<float>(value =>
                            {
                                proc = value;
                            });
                            new Thread(new Task(() =>
                            {
                                while (isExtracting)
                                {
                                    bar.Update((int)proc);
                                    if (proc >= 99.9f)
                                        break;
                                    Thread.Sleep(500);
                                }
                            }).Start).Start();
                            await Functions.ExtractArchive("./" + split[1], "./", extractProgress);
                            bar.Update(100);
                            isExtracting = false;
                            await Task.Delay(1000);
                            bar.Update(100);
                            Console.WriteLine("\n");
                            System.IO.File.Delete("./" + split[1]);



                        }

                        if (name == "DBUI")
                        {
                            Console.WriteLine("Reload with GUI ?[y/n]");
                            if (Console.ReadKey().Key == ConsoleKey.Y)
                            {
                                Process.Start("./DiscordBotGUI.exe");
                                Environment.Exit(0);
                            }
                        }
                    }
                    Console.WriteLine();
                }

            });


            AddCommand("value", "read value from VariableStack", (args) =>
                {
                    if (args.Length != 2) return;
                    if (!Config.ContainsKey(args[1])) return;

                    string data = Config.GetValue(args[1]);
                    Console.WriteLine($"{args[1]} => {data}");
                }
            );

            AddCommand("addv", "add variable to the system variables", async (args) =>
                {
                    if (args.Length < 3) return;
                    string in1 = args[1];
                    if (!Config.ContainsKey(in1))
                        Config.AddValueToVariables(in1, Functions.MergeStrings(args, 2), false);
                    else
                        Config.SetValue(in1, Functions.MergeStrings(args, 2));

                    Console.WriteLine($"Updated config file with the following command: {in1} => {Config.GetValue(in1)}");
                    Config.SaveConfig();
                }
            );

            AddCommand("remv", "remove variable from system variables", (args) =>
                {
                    if (args.Length < 2) return;
                    Config.RemoveKey(args[1]);
                    Config.SaveConfig();
                }
            );

            AddCommand("vars", "Display all variables", () =>
                {
                    var            d    = Config.GetAllVariables();
                    List<string[]> data = new List<string[]>();
                    data.Add(new string[] { "-", "-" });
                    data.Add(new string[] { "Key", "Value" });
                    data.Add(new string[] { "-", "-" });
                    foreach (var kvp in d) data.Add(new string[] { kvp.Key, kvp.Value });
                    data.Add(new string[] { "-", "-" });
                    Console_Utilities.FormatAndAlignTable(data);
                }
            );

            AddCommand("sd", "Shuts down the discord bot", async () =>
                {
                    await client.StopAsync();
                    await client.DisposeAsync();
                    Config.SaveConfig();
                    Environment.Exit(0);
                }
            );
        }

        public static void AddCommand(string command, string description, Action<string[]> action)
        {
            commandList.Add(new Tuple<string, string, Action<string[]>>(command, description, action));
            Console.ForegroundColor = ConsoleColor.White;
            Console_Utilities.WriteColorText($"Command &r{command} &cadded to the list of commands");
        }

        public static void AddCommand(string command, string description, Action action)
        {
            AddCommand(command, description, (args) => action());
        }

        public static void RemoveCommand(string command)
        {
            commandList.RemoveAll(x => x.Item1 == command);
        }

        public static Tuple<string, string, Action<string[]>>? SearchCommand(string command)
        {
            return commandList.FirstOrDefault(t => t.Item1 == command);
        }

        public void HandleCommand(string command)
        {
            string[] args = command.Split(' ');
            foreach (var item in commandList.ToList())
            {
                if (item.Item1 == args[0])
                {
                    item.Item3(args);
                    //Console.WriteLine($"Executing: {args[0]} with the following parameters: {args.MergeStrings(1)}");
                }
            }
        }

    }
}
