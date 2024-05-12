using DiscordBotCore.Online;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using DiscordBotCore.Others.Logger;
using DiscordBotCore.Plugin;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

        
namespace DiscordBotCore
{
    public class Application
    {
        public static Application CurrentApplication { get; private set; } = null;

        private static readonly string _DefaultLogMessageFormat = "{SenderName} {Message}";
        private static readonly string _ConfigFile = "./Data/Resources/config.json";
        private static readonly string _PluginsDatabaseFile = "./Data/Resources/plugins.json";

        private static readonly string _ResourcesFolder = "./Data/Resources";
        private static readonly string _PluginsFolder = "./Data/Plugins";
        private static readonly string _ArchivesFolder = "./Data/Archives";
        private static readonly string _LogsFolder = "./Data/Logs";

        private static readonly string _MaxParallelDownloads = "3";

        public string ServerID => ApplicationEnvironmentVariables["ServerID"];
        public string PluginDatabase => ApplicationEnvironmentVariables["PluginDatabase"];
        public string LogFile => $"{ApplicationEnvironmentVariables["LogFolder"]}/{DateTime.Now.ToLongDateString().Replace(" / ", "")}.log";
        

        public SettingsDictionary<string, string> ApplicationEnvironmentVariables { get; private set; }
        public InternalActionManager InternalActionManager { get; private set; }
        public PluginManager PluginManager { get; private set; }
        public Logger Logger { get; private set; }
        public Bot.Boot DiscordBotClient { get; internal set; }

        public static async Task CreateApplication()
        {
            if (CurrentApplication is not null)
                return;

            CurrentApplication = new Application();

            Directory.CreateDirectory(_ResourcesFolder);
            Directory.CreateDirectory(_PluginsFolder);
            Directory.CreateDirectory(_ArchivesFolder);
            Directory.CreateDirectory(_LogsFolder);

            CurrentApplication.ApplicationEnvironmentVariables = new SettingsDictionary<string, string>(_ConfigFile);
            bool result = await CurrentApplication.ApplicationEnvironmentVariables.LoadFromFile();
            if(!result)
            {
                PopulateEnvWithDefault();
                File.Delete(_ConfigFile);
                await CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();
            }

            CurrentApplication.Logger = CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("LogMessageFormat") ?
                                        new Logger(CurrentApplication.ApplicationEnvironmentVariables["LogMessageFormat"]) :
                                        new Logger(_DefaultLogMessageFormat);
            
            if (!File.Exists(_PluginsDatabaseFile))
            {
                List<PluginInfo> plugins = new();
                await JsonManager.SaveToJsonFile(_PluginsDatabaseFile, plugins);
            }

            CurrentApplication.PluginManager = new PluginManager();
            await CurrentApplication.PluginManager.UninstallMarkedPlugins();
            await CurrentApplication.PluginManager.CheckForUpdates();

            CurrentApplication.InternalActionManager = new InternalActionManager(CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"], "*.dll");
            await CurrentApplication.InternalActionManager.Initialize();
        }


        private static void PopulateEnvWithDefault()
        {
            if (CurrentApplication is null)
                return;

            if (CurrentApplication.ApplicationEnvironmentVariables is null)
                return;


            CurrentApplication.ApplicationEnvironmentVariables["LogFolder"] = _LogsFolder;
            CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"] = _PluginsFolder;
            CurrentApplication.ApplicationEnvironmentVariables["ArchiveFolder"] = _ArchivesFolder;
            CurrentApplication.ApplicationEnvironmentVariables["PluginDatabase"] = _PluginsDatabaseFile;
            CurrentApplication.ApplicationEnvironmentVariables["LogMessageFormat"] = _DefaultLogMessageFormat;
            CurrentApplication.ApplicationEnvironmentVariables["MaxParallelDownloads"] = _MaxParallelDownloads;
        }

    }
}
