using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using PluginManager.Bot;
using PluginManager.Interfaces.Updater;
using PluginManager.Online;
using PluginManager.Others;
using PluginManager.Others.Actions;
using PluginManager.Others.Logger;
using PluginManager.Plugin;
using PluginManager.Updater.Application;

namespace PluginManager;

public class Config
{

    private static readonly string _DefaultBranchForPlugins = "releases";

    private static readonly string _ConfigFile = "./Data/Resources/config.json";
    private static readonly string _PluginsDatabaseFile = "./Data/Resources/plugins.json";
    private static readonly string _ResourcesFolder = "./Data/Resources";
    private static readonly string _PluginsFolder = "./Data/Plugins";
    private static readonly string _ArchivesFolder = "./Data/Archives";
    private static readonly string _LogsFolder = "./Data/Logs";

    private static bool _isLoaded;
    public static Logger Logger;
    public static SettingsDictionary<string, string> AppSettings;

    internal static string PluginDatabase => AppSettings["PluginDatabase"];
    internal static string ServerID => AppSettings["ServerID"]; 

    public static InternalActionManager InternalActionManager;

    public static PluginsManager PluginsManager;

    internal static Boot? DiscordBotClient;

    public static Boot? DiscordBot => DiscordBotClient;

    public static async Task Initialize()
    {
        if (_isLoaded) return;

        Directory.CreateDirectory(_ResourcesFolder);
        Directory.CreateDirectory(_PluginsFolder);
        Directory.CreateDirectory(_ArchivesFolder);
        Directory.CreateDirectory(_LogsFolder);

        AppSettings = new SettingsDictionary<string, string>(_ConfigFile);
        bool response = await AppSettings.LoadFromFile();

        if (!response)
            throw new Exception("Invalid config file");

        AppSettings["LogFolder"] = _LogsFolder;
        AppSettings["PluginFolder"]  = _PluginsFolder;
        AppSettings["ArchiveFolder"] = _ArchivesFolder;

        AppSettings["PluginDatabase"] = _PluginsDatabaseFile;

        if (!File.Exists(_PluginsDatabaseFile))
        {
            List<PluginInfo> plugins = new();
            await JsonManager.SaveToJsonFile(_PluginsDatabaseFile, plugins);
        }

        Logger = new Logger(false, true, _LogsFolder + $"/{DateTime.Today.ToShortDateString().Replace("/", "")}.log");

        PluginsManager = new PluginsManager(_DefaultBranchForPlugins);

        await PluginsManager.UninstallMarkedPlugins();

        await PluginsManager.CheckForUpdates();

        _isLoaded = true;

        Logger.Log("Config initialized", typeof(Config));

    }

}
