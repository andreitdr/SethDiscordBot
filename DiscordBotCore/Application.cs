using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using DiscordBotCore.Bot;
using DiscordBotCore.Interfaces.Logger;
using DiscordBotCore.Online;
using DiscordBotCore.Online.Helpers;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using DiscordBotCore.Others.Settings;
using DiscordBotCore.Plugin;
using DiscordBotCore.Logging;
using DiscordBotCore.Repository;

namespace DiscordBotCore
{
    /// <summary>
    /// The main Application and its components
    /// </summary>
    public sealed class Application
    {
        /// <summary>
        /// Defines the current application. This is a singleton class
        /// </summary>
        public static Application CurrentApplication { get; private set; } = null!;
        
        public static bool IsRunning { get; private set; }
        
        private static readonly string _ConfigFile = "./Data/Resources/config.json";
        private static readonly string _PluginsDatabaseFile = "./Data/Resources/plugins.json";

        private static readonly string _ResourcesFolder = "./Data/Resources";
        private static readonly string _PluginsFolder = "./Data/Plugins";
        private static readonly string _LogsFolder = "./Data/Logs";
        
        private static readonly string _LogFormat = "{ThrowTime} {SenderName} {Message}";
        
        public DiscordBotApplication DiscordBotClient { get; set; } = null!;

        public List<ulong> ServerIDs => ApplicationEnvironmentVariables.GetList("ServerID", new List<ulong>());
        public string PluginDatabase => ApplicationEnvironmentVariables.Get<string>("PluginDatabase", _PluginsDatabaseFile);
        public CustomSettingsDictionary ApplicationEnvironmentVariables { get; private set; } = null!;
        public InternalActionManager InternalActionManager { get; private set; } = null!;
        public PluginManager PluginManager { get; private set; } = null!;
        public ILogger Logger { get; private set; } = null!;

        /// <summary>
        /// Create the application. This method is used to initialize the application. Can not initialize multiple times.
        /// </summary>
        public static async Task CreateApplication()
        {
            if (!await OnlineFunctions.IsInternetConnected())
            {
                Console.WriteLine("No internet connection detected. Exiting ...");
                Environment.Exit(0);
            }

            if (CurrentApplication is not null)
            {
                CurrentApplication.Logger.Log("Application is already initialized. Reinitialization is not allowed", LogType.Error);
                return;
            }

            CurrentApplication = new Application();

            Directory.CreateDirectory(_ResourcesFolder);
            Directory.CreateDirectory(_PluginsFolder);
            Directory.CreateDirectory(_LogsFolder);

            CurrentApplication.ApplicationEnvironmentVariables = await CustomSettingsDictionary.CreateFromFile(_ConfigFile, true);

            CurrentApplication.ApplicationEnvironmentVariables.Add("PluginFolder", _PluginsFolder);
            CurrentApplication.ApplicationEnvironmentVariables.Add("ResourceFolder", _ResourcesFolder);
            CurrentApplication.ApplicationEnvironmentVariables.Add("LogsFolder", _LogsFolder);
            
            CurrentApplication.Logger = new Logger(_LogsFolder, _LogFormat);
            
             if (!File.Exists(_PluginsDatabaseFile))
            {
                List<PluginInfo> plugins = new();
                await JsonManager.SaveToJsonFile(_PluginsDatabaseFile, plugins);
            }
            
            CurrentApplication.PluginManager = new PluginManager(PluginRepository.SolveRepo());

            await CurrentApplication.PluginManager.UninstallMarkedPlugins();
            await CurrentApplication.PluginManager.CheckForUpdates();

            CurrentApplication.InternalActionManager = new InternalActionManager();
            await CurrentApplication.InternalActionManager.Initialize();

            IsRunning = true;
        }
        public static string GetResourceFullPath(string path)
        {
            var result = Path.Combine(_ResourcesFolder, path);
            return result;
        }
        
        public static string GetPluginFullPath(string path)
        {
            var result = Path.Combine(_PluginsFolder, path);
            return result;
        }
    }
}
