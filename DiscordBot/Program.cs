using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using Discord;

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
    private static ConsoleCommandsHandler consoleCommandsHandler;

    /// <summary>
    ///     The main entry point for the application.
    /// </summary>
    [STAThread]
    [Obsolete]
    public static void Main(string[] args)
    {
        Console.WriteLine("Loading resources ...");

        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins/Commands");
        Directory.CreateDirectory("./Data/Plugins/Events");
        PreLoadComponents().Wait();
        do
        {
            if (!Config.ContainsKey("ServerID"))
            {


                Console.WriteLine("Please enter the server ID: ");
                Console_Utilities.WriteColorText("You can find it in the Server Settings at &r\"Widget\"&c section");
                Console.WriteLine("Example: 1234567890123456789");

                Console.WriteLine("This is not required, but is recommended. If you refuse to provide the ID, just press enter.\nThe server id is required to make easier for the bot to interact with the server.\nRemember: this bot is for one server ONLY.");
                Console.Write("User Input > ");
                ConsoleKeyInfo key = Console.ReadKey();
                if (key.Key == ConsoleKey.Enter)
                    Config.AddValueToVariables("ServerID", "null", false);
                else
                {
                    string SID = key.KeyChar + Console.ReadLine();
                    if (SID.Length != 18)
                    {
                        Console.Clear();
                        Console_Utilities.WriteColorText("&rYour server ID is not 18 characters long. Please try again. \n");

                        continue;
                    }
                    Config.AddValueToVariables("ServerID", SID, false);
                }
            }

            if (!Config.ContainsKey("token") || Config.GetValue<string>("token") == null || (Config.GetValue<string>("token")?.Length != 70 && Config.GetValue<string>("token")?.Length != 59))
            {
                Console.WriteLine("Please insert your token");
                Console.Write("Token = ");
                var token = Console.ReadLine();
                if (token?.Length == 59 || token?.Length == 70)
                    Config.AddValueToVariables("token", token, true);
                else
                {
                    Console.Clear();
                    Console_Utilities.WriteColorText("&rThe token length is invalid !");
                    continue;
                }
            }

            if (!Config.ContainsKey("prefix") || Config.GetValue<string>("prefix") == null || Config.GetValue<string>("prefix")?.Length != 1)
            {
                Console.WriteLine("Please insert your prefix (max. 1 character long):");
                Console.WriteLine("For a prefix longer then one character, the first character will be saved and the others will be ignored.\n No spaces, numbers, '/' or '\\' allowed");
                Console.Write("Prefix = ");
                var prefix = Console.ReadLine()![0];

                if (prefix == ' ' || char.IsDigit(prefix) || prefix == '/' || prefix == '\\')
                {
                    Console.Clear();
                    Console_Utilities.WriteColorText("&rThe prefix is invalid");
                    continue;
                }
                Config.AddValueToVariables("prefix", prefix.ToString(), false);
            }

            break;
        } while (true);

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

        if (len > 0 && args[0] == "/test")
        {
            int p = 1;
            bool allowed = true;
            Console.CancelKeyPress += (sender, e) => allowed = false;
            Console_Utilities.ProgressBar bar = new(ProgressBarType.NO_END);// { NoColor = false, Color = ConsoleColor.DarkRed };
            Console.WriteLine("Press Ctrl + C to stop.");
            while (p <= int.MaxValue - 1 && allowed)
            {
                bar.Update(100 / p);
                await Task.Delay(100);
                p++;
            }

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
        main.Stop();
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
                            Process.Start("./Updater/Updater.exe", $"/update {url} ./DiscordBot.exe ./");
                        }
                        else
                        {
                            string url = $"https://github.com/Wizzy69/SethDiscordBot/releases/download/v{newVersion}/net6.0_linux.zip";
                            Process.Start("./Updater/Updater", $"/update {url} ./DiscordBot ./");
                        }
                        Environment.Exit(0);
                    }

                    break;
                case "UpdaterVersion":
                    string updaternewversion = s[1];
                    if (Config.UpdaterVersion != updaternewversion)
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
                        bar.Stop();
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
