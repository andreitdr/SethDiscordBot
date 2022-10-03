using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using DiscordBot.Discord.Core;

using PluginManager;

using PluginManager.Items;
using PluginManager.Online;
using PluginManager.Others;

using Terminal.Gui;

namespace DiscordBot;

public class Program
{
    private static bool loadPluginsOnStartup;
    private static bool listPluginsAtStartup;
    private static ConsoleCommandsHandler consoleCommandsHandler;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    [Obsolete]
    public static void Main(string[] args)
    {

        Console.WriteLine("Loading resources ...");
        PreLoadComponents().Wait();


        if (!Config.ContainsKey("ServerID") || (!Config.ContainsKey("token") || Config.GetValue<string>("token") == null || (Config.GetValue<string>("token")?.Length != 70 && Config.GetValue<string>("token")?.Length != 59)) || (!Config.ContainsKey("prefix") || Config.GetValue<string>("prefix") == null || Config.GetValue<string>("prefix")?.Length != 1) || (args.Length > 0 && args[0] == "/newconfig"))
        {
            Application.Init();
            var top = Application.Top;

            //Application.IsMouseDisabled = true;
            var win = new Window("Discord Bot Config - " + Assembly.GetExecutingAssembly().GetName().Version)
            {
                X = 0,
                Y = 1,
                Width = Dim.Fill(),
                Height = Dim.Fill()
            };

            top.Add(win);

            var labelInfo = new Label("Configuration file not found or invalid. Please fill the following fields to create a new configuration file.")
            {
                X = Pos.Center(),
                Y = 2
            };


            var labelToken = new Label("Please insert your token here: ")
            {
                X = 5,
                Y = 5
            };

            var textFiledToken = new TextField("")
            {
                X = Pos.Left(labelToken) + labelToken.Text.Length + 2,
                Y = labelToken.Y,
                Width = 70
            };

            var labelPrefix = new Label("Please insert your prefix here: ")
            {
                X = 5,
                Y = 8
            };
            var textFiledPrefix = new TextField("")
            {
                X = Pos.Left(labelPrefix) + labelPrefix.Text.Length + 2,
                Y = labelPrefix.Y,
                Width = 1
            };

            var labelServerid = new Label("Please insert your server id here (optional): ")
            {
                X = 5,
                Y = 11,

            };
            var textFiledServerID = new TextField("")
            {
                X = Pos.Left(labelServerid) + labelServerid.Text.Length + 2,
                Y = labelServerid.Y,
                Width = 18
            };

            var button = new Button("Submit")
            {
                X = Pos.Center() - 10,
                Y = 16
            };

            var button2 = new Button("License")
            {
                X = Pos.Center() + 10,
                Y = 16
            };

            Console.CancelKeyPress += (sender, e) =>
            {
                top.Running = false;

            };

            button.Clicked += () =>
            {
                string passMessage = "";
                if (textFiledToken.Text.Length != 70 && textFiledToken.Text.Length != 59)
                    passMessage += "Invalid token, ";
                if (textFiledPrefix.Text.ContainsAny("0123456789/\\ ") || textFiledPrefix.Text.Length != 1)
                    passMessage += "Invalid prefix, ";
                if (textFiledServerID.Text.Length != 18 && textFiledServerID.Text.Length > 0)
                    passMessage += "Invalid serverID";

                if (passMessage != "")
                {
                    MessageBox.ErrorQuery("Discord Bot Settings", "Failed to pass check. Invalid information given:\n" + passMessage, "Retry");
                    return;
                }


                Config.AddValueToVariables<string>("ServerID", ((string)textFiledServerID.Text), true);
                Config.AddValueToVariables<string>("token", ((string)textFiledToken.Text), true);
                Config.AddValueToVariables<string>("prefix", ((string)textFiledPrefix.Text), true);

                MessageBox.Query("Discord Bot Settings", "Successfully saved config !\nJust start the bot :D", "Start :D");
                top.Running = false;


            };

            button2.Clicked += async () =>
            {
                List<string> license = await ServerCom.ReadTextFromURL("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/LICENSE.txt");
                string ProductLicense = "Seth Discord Bot\n\nDeveloped by Wizzy#9181\nThis application can be used and modified by anyone. Plugin development for this application is also free and supported";
                int r = MessageBox.Query("Discord Bot Settings", ProductLicense, "Close", "Read about libraries used");
                if (r == 1)
                {
                    int i = 0;
                    while (i < license.Count)
                    {
                        string print_message = license[i++] + "\n";
                        for (; i < license.Count && !license[i].StartsWith("-----------"); i++)
                            print_message += license[i] + "\n";
                        if (MessageBox.Query("Licenses", print_message, "Next", "Quit") == 1) break;
                    }

                }
            };

            win.Add(labelInfo, labelPrefix, labelServerid, labelToken);
            win.Add(textFiledToken, textFiledPrefix, textFiledServerID);
            win.Add(button, button2);
            Application.Run();
            Application.Shutdown();
        }

        HandleInput(args).Wait();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    /// <param name="discordbooter">The discord booter used to start the application</param>
    private static void NoGUI(Boot discordbooter)
    {

#if DEBUG
        Console.WriteLine();
        ConsoleCommandsHandler.ExecuteCommad("lp").Wait();
#else
        if (loadPluginsOnStartup) consoleCommandsHandler.HandleCommand("lp");
        if (listPluginsAtStartup) consoleCommandsHandler.HandleCommand("listplugs");
#endif
        Config.SaveConfig(SaveType.NORMAL).Wait();

        while (true)
        {

            var cmd = Console.ReadLine();
            if (!consoleCommandsHandler.HandleCommand(cmd!
#if DEBUG
               , false
#endif

             ) && cmd.Length > 0)
                Console.WriteLine("Failed to run command " + cmd);
        }
    }

    /// <summary>
    ///     Start the bot without user interface
    /// </summary>
    /// <returns>Returns the boot loader for the Discord Bot</returns>
    private static async Task<Boot> StartNoGUI()
    {
        Console.Clear();
        Console.ForegroundColor = ConsoleColor.DarkYellow;

        List<string> startupMessageList = await ServerCom.ReadTextFromURL("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/StartupMessage");

        foreach (var message in startupMessageList)
            Console.WriteLine(message);

        Console.WriteLine($"Running on version: {Config.GetValue<string>("Version") ?? System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString()}");
        Console.WriteLine($"Git URL: {Config.GetValue<string>("GitURL") ?? " Could not find Git URL"}");

        Console_Utilities.WriteColorText("&rRemember to close the bot using the ShutDown command (&ysd&r) or some settings won't be saved\n");
        Console.ForegroundColor = ConsoleColor.White;

        if (Config.ContainsKey("LaunchMessage"))
        {
            Console_Utilities.WriteColorText(Config.GetValue<string>("LaunchMessage"));
            Config.RemoveKey("LaunchMessage");
        }

        Console_Utilities.WriteColorText("Please note that the bot saves a backup save file every time you are using the shudown command (&ysd&c)");
        Console.WriteLine($"============================ LOG ============================");

        try
        {
            var token = Config.GetValue<string>("token");
#if DEBUG
            Console.WriteLine("Starting in DEBUG MODE");
            if (!Directory.Exists("./Data/BetaTest"))
                Console.WriteLine("Failed to start in debug mode because the folder ./Data/BetaTest does not exist");
            else
            {
                token = File.ReadAllText("./Data/BetaTest/token.txt");

                //Debug mode code...
            }
#endif

            var prefix = Config.GetValue<string>("prefix");
            var discordbooter = new Boot(token, prefix);
            await discordbooter.Awake();
            return discordbooter;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex);
            return null;
        }
    }

    /// <summary>
    ///     Clear folder
    /// </summary>
    /// <param name="d">Directory path</param>
    private static Task ClearFolder(string d)
    {
        var files = Directory.GetFiles(d);
        var fileNumb = files.Length;
        for (var i = 0; i < fileNumb; i++)
        {
            File.Delete(files[i]);
            Console.WriteLine("Deleting : " + files[i]);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Handle user input arguments from the startup of the application
    /// </summary>
    /// <param name="args">The arguments</param>
    private static async Task HandleInput(string[] args)
    {

        var len = args.Length;

        if (len == 3 && args[0] == "/download")
        {
            var url = args[1];
            var location = args[2];

            await ServerCom.DownloadFileAsync(url, location);

            return;
        }

        if (len > 0 && (args.Contains("--cmd") || args.Contains("--args") || args.Contains("--nomessage")))
        {
            if (args.Contains("lp") || args.Contains("loadplugins"))
                loadPluginsOnStartup = true;
            if (args.Contains("listplugs"))
                listPluginsAtStartup = true;

            len = 0;
        }

        if (len == 2 && args[0] == "/procKill")
        {
            Process.GetProcessById(int.Parse(args[1])).Kill();
            len = 0;
        }


        var b = await StartNoGUI();
        consoleCommandsHandler = new ConsoleCommandsHandler(b.client);

        if (len > 0 && args[0] == "/remplug")
        {

            string plugName = Functions.MergeStrings(args, 1);
            Console.WriteLine("Starting to remove " + plugName);
            await ConsoleCommandsHandler.ExecuteCommad("remplug " + plugName);
            loadPluginsOnStartup = true;
            len = 0;
        }

        if (len > 0 && args[0] == "/updateplug")
        {
            string plugName = args.MergeStrings(1);
            Console.WriteLine("Updating " + plugName);
            await ConsoleCommandsHandler.ExecuteCommad("dwplug" + plugName);
            return;
        }

        if (len == 0 || (args[0] != "--exec" && args[0] != "--execute"))
        {

            Thread mainThread = new Thread(() =>
            {
                try
                {
                    NoGUI(b);
                }
                catch (IOException ex)
                {
                    if (ex.Message == "No process is on the other end of the pipe." || (uint)ex.HResult == 0x800700E9)
                    {
                        if (!Config.ContainsKey("LaunchMessage"))
                            Config.AddValueToVariables("LaunchMessage", "An error occured while closing the bot last time. Please consider closing the bot using the &rsd&c method !\nThere is a risk of losing all data or corruption of the save file, which in some cases requires to reinstall the bot !", false);
                        Functions.WriteErrFile(ex.ToString());
                    }
                }



            });
            mainThread.Start();
            return;
        }


        Console.ForegroundColor = ConsoleColor.DarkYellow;
        Console.WriteLine("Execute command interface noGUI\n\n");
        Console.WriteLine(
            "\tCommand name\t\t\t\tDescription\n" +
            "-- help | -help\t\t ------ \tDisplay the help message\n" +
            "--reset-full\t\t ------ \tReset all files (clear files)\n" +
            "--reset-logs\t\t ------ \tClear up the output folder\n" +
            "--start\t\t ------ \tStart the bot\n" +
            "exit\t\t\t ------ \tClose the application"
        );
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            Console.Write("> ");
            var message = Console.ReadLine().Split(' ');

            switch (message[0])
            {
                case "--help":
                case "-help":
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine("\tCommand name\t\t\t\tDescription\n" + "-- help | -help\t\t ------ \tDisplay the help message\n" + "--reset-full\t\t ------ \tReset all files (clear files)\n" + "--reset-settings\t ------ \tReset only bot settings\n" + "--reset-logs\t\t ------ \tClear up the output folder\n" + "--start\t\t ------ \tStart the bot\n" + "exit\t\t\t ------ \tClose the application");
                    break;
                case "--reset-full":
                    await ClearFolder("./Data/Resources/");
                    await ClearFolder("./Output/Logs/");
                    await ClearFolder("./Output/Errors");
                    await ClearFolder("./Data/Languages/");
                    await ClearFolder("./Data/Plugins/Commands");
                    await ClearFolder("./Data/Plugins/Events");
                    Console.WriteLine("Successfully cleared all folders");
                    break;
                case "--reset-logs":
                    await ClearFolder("./Output/Logs");
                    await ClearFolder("./Output/Errors");
                    Console.WriteLine("Successfully clear logs folder");
                    break;
                case "--exit":
                case "exit":
                    Environment.Exit(0);
                    break;

                default:
                    Console.WriteLine("Failed to execute command " + message[0]);
                    break;
            }
        }
    }

    private static async Task PreLoadComponents()
    {
        Console_Utilities.ProgressBar main = new Console_Utilities.ProgressBar(ProgressBarType.NO_END);
        main.Start();
        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins/Commands");
        Directory.CreateDirectory("./Data/Plugins/Events");
        Directory.CreateDirectory("./Data/PAKS");
        await Config.LoadConfig();
        if (Config.ContainsKey("DeleteLogsAtStartup"))
            if (Config.GetValue<bool>("DeleteLogsAtStartup"))
                foreach (var file in Directory.GetFiles("./Output/Logs/"))
                    File.Delete(file);
        List<string> OnlineDefaultKeys = await ServerCom.ReadTextFromURL("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/SetupKeys");

        Config.PluginConfig.Load();

        if (!Config.ContainsKey("Version"))
            Config.AddValueToVariables("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString(), false);
        else
            Config.SetValue("Version", Assembly.GetExecutingAssembly().GetName().Version.ToString());

        foreach (var key in OnlineDefaultKeys)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;
            string[] s = key.Split(' ');
            try
            {
                if (Config.ContainsKey(s[0])) Config.SetValue(s[0], s[1]);
                else Config.GetAndAddValueToVariable(s[0], s[1], s[2].Equals("true", StringComparison.CurrentCultureIgnoreCase));
            }
            catch (Exception ex)
            {
                Functions.WriteErrFile(ex.Message);
            }
        }




        List<string> onlineSettingsList = await ServerCom.ReadTextFromURL("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/OnlineData");
        main.Stop("Loaded online settings. Loading updates ...");
        foreach (var key in onlineSettingsList)
        {
            if (key.Length <= 3 || !key.Contains(' ')) continue;

            string[] s = key.Split(' ');
            switch (s[0])
            {
                case "CurrentVersion":
                    string newVersion = s[1];
                    if (!newVersion.Equals(Config.GetValue<string>("Version")))
                    {
                        if (Functions.GetOperatingSystem() == PluginManager.Others.OperatingSystem.WINDOWS)
                        {

                            string url = $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}/net6.0.zip";
                            //string url2 = $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}-preview/net6.0.zip";

                            Process.Start(".\\Updater\\Updater.exe", $"{newVersion} {url} {Process.GetCurrentProcess().ProcessName}");

                        }
                        else
                        {
                            string url = $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}/net6.0_linux.zip";
                            Console.WriteLine("Downloading update ...");
                            await ServerCom.DownloadFileNoProgressAsync(url, "./update.zip");
                            await File.WriteAllTextAsync("Install.sh", "#!/bin/bash\nunzip -qq update.zip -d ./\nrm update.zip\nchmod +x SethDiscordBot\n./DiscordBot");
                            Process.Start("Install.sh").WaitForExit();
                            Environment.Exit(0);

                        }
                    }

                    break;
                case "UpdaterVersion":
                    string updaternewversion = s[1];
                    if (Config.UpdaterVersion != updaternewversion && Functions.GetOperatingSystem() == PluginManager.Others.OperatingSystem.WINDOWS)
                    {
                        Console.Clear();
                        Console.WriteLine("Installing updater ...\nDo NOT close the bot during update !");
                        Console_Utilities.ProgressBar bar = new Console_Utilities.ProgressBar(ProgressBarType.NO_END);
                        bar.Start();
                        await ServerCom.DownloadFileNoProgressAsync("https://github.com/Wizzy69/installer/releases/download/release-1-discordbot/Updater.zip", "./Updater.zip");
                        await Functions.ExtractArchive("./Updater.zip", "./", null, UnzipProgressType.PercentageFromTotalSize);
                        Config.UpdaterVersion = updaternewversion;
                        File.Delete("Updater.zip");
                        await Config.SaveConfig(SaveType.NORMAL);
                        bar.Stop("Updater has been updated !");
                        Console.Clear();
                    }
                    break;
            }
        }


        Console_Utilities.Initialize();
        await Config.SaveConfig(SaveType.NORMAL);
        Console.Clear();
    }
}
