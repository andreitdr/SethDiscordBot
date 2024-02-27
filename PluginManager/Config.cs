using System;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Bot;
using PluginManager.Others;
using PluginManager.Others.Logger;
using OperatingSystem = System.OperatingSystem;

namespace PluginManager;

public class Config
{
    private static bool                               _isLoaded;
    public static  Logger                             Logger;
    public static  SettingsDictionary<string, string> AppSettings;

    internal static Boot? DiscordBotClient;

    public static Boot? DiscordBot => DiscordBotClient;

    public static async Task Initialize()
    {
        if (_isLoaded) return;

        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins");
        Directory.CreateDirectory("./Data/Archives");
        Directory.CreateDirectory("./Data/Logs");

        AppSettings = new SettingsDictionary<string, string>("./Data/Resources/config.json");

        AppSettings["LogFolder"]     = "./Data/Logs";
        AppSettings["PluginFolder"]  = "./Data/Plugins";
        AppSettings["ArchiveFolder"] = "./Data/Archives";

        if (OperatingSystem.IsLinux())
        {
            var windowManager = Environment.GetEnvironmentVariable("XDG_CURRENT_DESKTOP");
            AppSettings["UI"] = windowManager switch
            {
                "KDE"   => "KDE",
                "GNOME" => "GNOME",
                _       => "CONSOLE"
            };
        }
        else AppSettings["UI"] = "CONSOLE";

        Logger = new Logger(false, true,
            AppSettings["LogFolder"] + $"/{DateTime.Today.ToShortDateString().Replace("/", "")}.log"
        );

        ArchiveManager.Initialize();

        UX.UxHandler.Init();
        _isLoaded = true;


        Logger.Log("Config initialized", typeof(Config));


    }

}
