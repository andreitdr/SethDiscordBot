using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBot.Discord.Core;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Items;
using PluginManager.Online;
using PluginManager.Others;

namespace DiscordBot;

public class Program
{
    private static bool loadPluginsOnStartup;
    private static bool listPluginsAtStartup;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    [Obsolete]
    public static void Main(string[] args)
    {
        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins/Commands");
        Directory.CreateDirectory("./Data/Plugins/Events");
        PreLoadComponents().Wait();

        if (!Config.ContainsKey("token") || Config.GetValue<string>("token") == null || Config.GetValue<string>("token")?.Length != 70)
        {
            Console.WriteLine("Please insert your token");
            Console.Write("Token = ");
            var token = Console.ReadLine();
            if (token?.Length == 59 || token?.Length == 70)
                Config.AddValueToVariables("token", token, true);
            else
                Console.WriteLine("Invalid token");

            Console.WriteLine("Please insert your prefix (max. 1 character long):");
            Console.WriteLine("For a prefix longer then one character, the first character will be saved and the others will be ignored.\n No spaces or numbers allowed");
            Console.Write("Prefix = ");
            var prefix = Console.ReadLine()![0];

            if (prefix == ' ' || char.IsDigit(prefix))
                return;
            Config.AddValueToVariables("prefix", prefix.ToString(), false);
        }

        if (!Config.ContainsKey("prefix") || Config.GetValue<string>("prefix") == default)
        {
            Console.WriteLine("Please insert your prefix (max. 1 character long):");
            Console.WriteLine("For a prefix longer then one character, the first character will be saved and the others will be ignored.\n No spaces or numbers allowed");
            Console.Write("Prefix = ");
            var prefix = Console.ReadLine()![0];
            if (prefix == ' ') return;
            Config.AddValueToVariables("prefix", prefix.ToString(), false);
        }


        HandleInput(args).Wait();
    }

    /// <summary>
    ///     The main loop for the discord bot
    /// </summary>
    /// <param name="discordbooter">The discord booter used to start the application</param>
    private static void NoGUI(Boot discordbooter)
    {
        var consoleCommandsHandler = new ConsoleCommandsHandler(discordbooter.client);
        if (loadPluginsOnStartup) consoleCommandsHandler.HandleCommand("lp");
        if (listPluginsAtStartup) consoleCommandsHandler.HandleCommand("listplugs");

        Config.SaveConfig();

        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;

#if DEBUG
            Console_Utilities.WriteColorText("&rSethBot (&yDEBUG&r) &c> ", false);
            var cmd = Console.ReadLine();
            if (!consoleCommandsHandler.HandleCommand(cmd!, false) && cmd.Length > 0)
                Console.WriteLine("Failed to run command " + cmd);
#else
            Console_Utilities.WriteColorText("&rSethBot &c> ", false);
            var cmd = Console.ReadLine();
            if (!consoleCommandsHandler.HandleCommand(cmd!) && cmd.Length > 0)
                Console.WriteLine("Failed to run command " + cmd);
#endif
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
        var files    = Directory.GetFiles(d);
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
            var url      = args[1];
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


        if (len == 0 || (args[0] != "--exec" && args[0] != "--execute"))
        {
            var    b          = await StartNoGUI();

            Thread mainThread = new Thread(() => NoGUI(b));
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
        await Config.LoadConfig();
        if (Config.ContainsKey("DeleteLogsAtStartup"))
            if (Config.GetValue<bool>("DeleteLogsAtStartup"))
                foreach (var file in Directory.GetFiles("./Output/Logs/"))
                    File.Delete(file);
        List<string> OnlineDefaultKeys = await ServerCom.ReadTextFromURL("https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/SetupKeys");

        Config.PluginConfig.Load();

        if (!Config.ContainsKey("Version"))
            Config.AddValueToVariables("Version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(), false);
        else
            Config.SetValue("Version", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString());

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
                        Console.WriteLine("A new version has been released on github page.");
                        Console.WriteLine("Download the new version using the following link wrote in yellow");
                        Console_Utilities.WriteColorText("&y" + Config.GetValue<string>("GitURL") + "&c");

                        Console.WriteLine();
                        Console.WriteLine("Your product will work just fine on this outdated version, but an update is recommended.\n" +
                                          "From now on, this version is no longer supported"
                        );
                        Console_Utilities.WriteColorText("&rUse at your own risk&c");

                        Console_Utilities.WriteColorText("&mCurrent Version: " + Config.GetValue<string>("Version") + "&c");
                        Console_Utilities.WriteColorText("&gNew Version: " + newVersion + "&c");

                        Console.WriteLine("\n\n");
                        await Task.Delay(1000);

                        int waitTime = 20; //wait time to proceed

                        Console.Write($"The bot will start in {waitTime} seconds");
                        while (waitTime > 0)
                        {
                            await Task.Delay(1000);
                            waitTime--;
                            Console.SetCursorPosition("The bot will start in ".Length, Console.CursorTop);
                            Console.Write("                         ");
                            Console.SetCursorPosition("The bot will start in ".Length, Console.CursorTop);
                            Console.Write(waitTime + " seconds");
                        }
                    }

                    break;
            }
        }

        Console_Utilities.Initialize();

        Config.SaveConfig();
    }
}
