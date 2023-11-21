using System;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Bot;
using PluginManager.Others;
using PluginManager.Others.Logger;

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

        AppSettings["LogFolder"] = "./Data/Logs";

        Logger = new Logger(false, true,
            AppSettings["LogFolder"] + $"/{DateTime.Today.ToShortDateString().Replace("/", "")}.log"
        );

        ArchiveManager.Initialize();

        _isLoaded = true;

        Logger.Log(message: "Config initialized", source: typeof(Config));
    }

}
