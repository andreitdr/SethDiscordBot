using DiscordBotCore.Interfaces.PluginManager;
using DiscordBotCore.Online;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using DiscordBotCore.Plugin;

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiscordBotCore.Others.Exceptions;
using DiscordBotCore.Interfaces.Logger;
using DiscordBotCore.Modules;
using System.Diagnostics;
using DiscordBotCore.Online.Helpers;
using DiscordBotCore.Others.Settings;


namespace DiscordBotCore
{
    /// <summary>
    /// The main Application and its components
    /// </summary>
    public sealed class Application
    {
        public static Application CurrentApplication { get; private set; } = null!;

        private static readonly string _ConfigFile = "./Data/Resources/config.json";
        private static readonly string _PluginsDatabaseFile = "./Data/Resources/plugins.json";

        private static readonly string _ResourcesFolder = "./Data/Resources";
        private static readonly string _PluginsFolder = "./Data/Plugins";
        private static readonly string _LogsFolder = "./Data/Logs";

        public List<ulong> ServerIDs => ApplicationEnvironmentVariables.GetList("ServerID", new List<ulong>());
        public string PluginDatabase => ApplicationEnvironmentVariables.Get<string>("PluginDatabase", _PluginsDatabaseFile);
        
        private ModuleManager _ModuleManager;
        
        public CustomSettingsDictionary ApplicationEnvironmentVariables { get; private set; }
        public InternalActionManager InternalActionManager { get; private set; }

        public ILogger Logger
        {
            get
            {
                try
                {
                    return _ModuleManager.GetModule<ILogger>();
                }
                catch (ModuleNotFoundException<ILogger> ex)
                {
                    Console.WriteLine("No logger found");
                    Console.WriteLine("Not having a valid logger is NOT an option. The default module will be downloaded from the official repo.");
                    Console.WriteLine("Install the default one ? [y/n]");
                    ConsoleKey response = Console.ReadKey().Key;
                    if (response is ConsoleKey.Y)
                    {
                        Process.Start("DiscordBot", "--module-install LoggerModule");
                    }

                    Environment.Exit(0);
                    return null!;
                }
            }
        }

        public IPluginManager PluginManager { get; private set; }
        public Bot.App DiscordBotClient { get; internal set; }

        public static async Task CreateApplication()
        {

            if (!await OnlineFunctions.IsInternetConnected())
            {
                Console.WriteLine("No internet connection detected. Exiting ...");
                Environment.Exit(0);
            }
            
            if (CurrentApplication is not null)
                return;

            CurrentApplication = new Application();
            
            Directory.CreateDirectory(_ResourcesFolder);
            Directory.CreateDirectory(_PluginsFolder);
            Directory.CreateDirectory(_LogsFolder);

            CurrentApplication.ApplicationEnvironmentVariables = await CustomSettingsDictionary.CreateFromFile(_ConfigFile, true);

            CurrentApplication.ApplicationEnvironmentVariables.Set("PluginFolder", _PluginsFolder);
            CurrentApplication.ApplicationEnvironmentVariables.Set("ResourceFolder", _ResourcesFolder);
            CurrentApplication.ApplicationEnvironmentVariables.Set("LogsFolder", _LogsFolder);
            
            
            CurrentApplication._ModuleManager = new ModuleManager();
            await CurrentApplication._ModuleManager.LoadModules();
            
            if (!File.Exists(_PluginsDatabaseFile))
            {
                List<PluginInfo> plugins = new();
                await JsonManager.SaveToJsonFile(_PluginsDatabaseFile, plugins);
            }

            
#if DEBUG
            CurrentApplication.PluginManager = new PluginManager("tests");
#else
            CurrentApplication.PluginManager = new PluginManager();
#endif
            await CurrentApplication.PluginManager.UninstallMarkedPlugins();
            await CurrentApplication.PluginManager.CheckForUpdates();

            CurrentApplication.InternalActionManager = new InternalActionManager();
            await CurrentApplication.InternalActionManager.Initialize();


        }

        public IReadOnlyDictionary<Type, List<object>> GetLoadedCoreModules() => _ModuleManager.LoadedModules.AsReadOnly();

        public static string GetResourceFullPath(string path)
        {
            string result = Path.Combine(_ResourcesFolder, path);
            return result;
        }

        public static string GetResourceFullPath() => CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ResourceFolder", _ResourcesFolder);

        public static string GetPluginFullPath(string path)
        {
            string result = Path.Combine(_PluginsFolder, path);
            return result;
        }

        public static string GetPluginFullPath() => CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder", _PluginsFolder);

        public static async Task<string> GetPluginDependencyPath(string dependencyName, string? pluginName = null)
        {
            string? dependencyLocation;
            if(pluginName is null)
                dependencyLocation = await Application.CurrentApplication.PluginManager.GetDependencyLocation(dependencyName);
            else
                dependencyLocation = await Application.CurrentApplication.PluginManager.GetDependencyLocation(dependencyName, pluginName);
            
            if(dependencyLocation is null)
                throw new DependencyNotFoundException($"Dependency {dependencyName} not found", pluginName);
            
            return dependencyLocation;
        }
    }
}
