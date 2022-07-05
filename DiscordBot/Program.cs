using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBot.Discord.Core;
using PluginManager;
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

            if (prefix == ' ' || char.IsDigit(prefix)) return;
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
    private static Task NoGUI(Boot discordbooter)
    {
        var consoleCommandsHandler = new ConsoleCommandsHandler(discordbooter.client);
        if (loadPluginsOnStartup) consoleCommandsHandler.HandleCommand("lp");
        if (listPluginsAtStartup) consoleCommandsHandler.HandleCommand("listplugs");
        Config.SaveConfig();
        while (true)
        {
            Console.ForegroundColor = ConsoleColor.White;
            var cmd = Console.ReadLine();
            consoleCommandsHandler.HandleCommand(cmd);
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
        Console.WriteLine("Discord BOT for Cross Platform");
        Console.WriteLine("Created by: Wizzy\nDiscord: Wizzy#9181");

        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine("============================ Discord BOT - Cross Platform ============================");

        try
        {
            var token  = Config.GetValue<string>("token");
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
            if (args.Contains("lp") || args.Contains("loadplugins")) loadPluginsOnStartup = true;
            if (args.Contains("listplugs")) listPluginsAtStartup                          = true;
            len = 0;
        }


        if (len == 0 || (args[0] != "--exec" && args[0] != "--execute"))
        {
            var b = await StartNoGUI();
            await NoGUI(b);
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
                    Console.WriteLine(
                        "\tCommand name\t\t\t\tDescription\n" +
                        "-- help | -help\t\t ------ \tDisplay the help message\n" +
                        "--reset-full\t\t ------ \tReset all files (clear files)\n" +
                        "--reset-settings\t ------ \tReset only bot settings\n" +
                        "--reset-logs\t\t ------ \tClear up the output folder\n" +
                        "--start\t\t ------ \tStart the bot\n" +
                        "exit\t\t\t ------ \tClose the application"
                    );
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
                    Console.WriteLine("Successfully cleard logs folder");
                    break;
                case "--exit":
                case "exit":
                    Environment.Exit(0);
                    break;
                case "--start":
                    var booter = await StartNoGUI();
                    await NoGUI(booter);
                    return;

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

        Config.Plugins.Load();
    }
}
