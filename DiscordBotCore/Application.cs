using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

using DiscordBotCore.Online;
using DiscordBotCore.Online.Helpers;

using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using DiscordBotCore.Others.Exceptions;
using DiscordBotCore.Others.Settings;

using DiscordBotCore.Modules;
using DiscordBotCore.Plugin;

using DiscordBotCore.Interfaces.PluginManager;
using DiscordBotCore.Interfaces.Modules;

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

        private static readonly string _ConfigFile = "./Data/Resources/config.json";
        private static readonly string _PluginsDatabaseFile = "./Data/Resources/plugins.json";

        private static readonly string _ResourcesFolder = "./Data/Resources";
        private static readonly string _PluginsFolder = "./Data/Plugins";
        private static readonly string _LogsFolder = "./Data/Logs";

        public List<ulong> ServerIDs => ApplicationEnvironmentVariables.GetList("ServerID", new List<ulong>());
        public string PluginDatabase => ApplicationEnvironmentVariables.Get<string>("PluginDatabase", _PluginsDatabaseFile);

        private ModuleManager _ModuleManager = null!;

        public CustomSettingsDictionary ApplicationEnvironmentVariables { get; private set; } = null!;
        public InternalActionManager InternalActionManager { get; private set; } = null!;
        public IPluginManager PluginManager { get; private set; } = null!;
        public Bot.App DiscordBotClient { get; internal set; } = null!;

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

            CurrentApplication.ApplicationEnvironmentVariables.Add("PluginFolder", _PluginsFolder);
            CurrentApplication.ApplicationEnvironmentVariables.Add("ResourceFolder", _ResourcesFolder);
            CurrentApplication.ApplicationEnvironmentVariables.Add("LogsFolder", _LogsFolder);

            CurrentApplication._ModuleManager = new ModuleManager();
            await CurrentApplication._ModuleManager.LoadModules();
            var requirements = await CurrentApplication._ModuleManager.CheckRequiredModules();
            await CurrentApplication._ModuleManager.SolveRequirementIssues(requirements);

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

        /// <summary>
        /// A special class that is designed to log messages. It is a wrapper around the Logger module
        /// The logger module is required to have this specific methods:
        /// <br/><br/>
        /// BaseLogException(Exception ex, object sender, bool fullStackTrace)<br/>
        /// BaseLog(string message)<br/>
        /// LogWithTypeAndFormat(string message, LogType logType, string format)<br/>
        /// LogWithType(string message, LogType logType)<br/>
        /// LogWithSender(string message, object sender)<br/>
        /// LogWithTypeAndSender(string message, object sender, LogType type)<br/>
        /// SetPrintFunction(Action[in string] outFunction)<br/><br/>
        /// 
        /// If your custom logger does not have the following methods mapped, the application might crash.
        /// Please check <b>modules.json</b> file for the mapping or refer to the official repository for the logger module.
        /// </summary>
        public static class Logger
        {
            private static readonly KeyValuePair<ModuleData, IModule> _LoggerModule = CurrentApplication._ModuleManager.GetModule(ModuleType.Logger);
            public static async void LogException(Exception ex, object sender, bool fullStackTrace = false)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["BaseLogException"], [ex, sender, fullStackTrace]);
            }

            public static async void Log(string message)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["BaseLog"], [message]);
            }

            public static async void Log(string message, LogType logType, string format)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithTypeAndFormat"], [message, logType, format]);
            }

            public static async void Log(string message, LogType logType)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithType"], [message, logType]);
            }

            public static async void Log(string message, object sender)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithSender"], [message, sender]);
            }

            public static async void Log(string message, object sender, LogType type)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithTypeAndSender"], [message, sender, type]);
            }

            public static async void SetOutFunction(Action<string> outFunction)
            {
                await CurrentApplication._ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["SetPrintFunction"], [outFunction]);
            }
        }

        public ReadOnlyDictionary<ModuleData, IModule> GetLoadedCoreModules() => _ModuleManager.Modules.AsReadOnly();

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
            if (pluginName is null)
                dependencyLocation = await Application.CurrentApplication.PluginManager.GetDependencyLocation(dependencyName);
            else
                dependencyLocation = await Application.CurrentApplication.PluginManager.GetDependencyLocation(dependencyName, pluginName);

            if (dependencyLocation is null)
                throw new DependencyNotFoundException($"Dependency {dependencyName} not found", pluginName);

            return dependencyLocation;
        }

        /// <summary>
        /// Invokes a method from a module
        /// </summary>
        public static async Task InvokeModuleMethod(string moduleName, string methodName, object[] parameters)
        {
            KeyValuePair<ModuleData, IModule> module = CurrentApplication._ModuleManager.GetModule(moduleName);
            await CurrentApplication._ModuleManager.InvokeMethod(module.Value, module.Value.MethodMapping[methodName], parameters);
        }

        /// <summary>
        /// Invokes a method from a module and returns the result
        /// </summary>
        public static async Task<object?> InvokeModuleMethodWithResponse(string moduleName, string methodName, object[] parameters)
        {
            KeyValuePair<ModuleData, IModule> module = CurrentApplication._ModuleManager.GetModule(moduleName);
            return await CurrentApplication._ModuleManager.InvokeMethodWithReturnValue(module.Value, module.Value.MethodMapping[methodName], parameters);
        }
    }
}
