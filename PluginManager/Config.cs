using System;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Bot;
using PluginManager.Others;
using PluginManager.Others.Logger;

namespace PluginManager;

public class Config
{
    private static bool                  IsLoaded;
    public static  DBLogger              Logger;
    public static  SettingsDictionary<string, string>? AppSettings;

    internal static Boot? _DiscordBotClient;

    public static Boot? DiscordBot => _DiscordBotClient;

    public static async Task Initialize()
    {
        if (IsLoaded) return;

        Directory.CreateDirectory("./Data/Resources");
        Directory.CreateDirectory("./Data/Plugins");
        Directory.CreateDirectory("./Data/PAKS");
        Directory.CreateDirectory("./Data/Logs/Logs");
        Directory.CreateDirectory("./Data/Logs/Errors");

        AppSettings = new SettingsDictionary<string, string>("./Data/Resources/config.json");

        AppSettings["LogFolder"]   = "./Data/Logs/Logs";
        AppSettings["ErrorFolder"] = "./Data/Logs/Errors";

        Logger = new DBLogger(true);

        ArchiveManager.Initialize();

        IsLoaded = true;

        Logger.Log("Config initialized", LogLevel.INFO);
    }
    
}
