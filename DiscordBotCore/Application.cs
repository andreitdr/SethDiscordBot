using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

using DiscordBotCore.Bot;
using DiscordBotCore.Online;
using DiscordBotCore.Online.Helpers;

using DiscordBotCore.Others;
using DiscordBotCore.Others.Actions;
using DiscordBotCore.Others.Exceptions;
using DiscordBotCore.Others.Settings;

using DiscordBotCore.Modules;
using DiscordBotCore.Plugin;

using DiscordBotCore.Interfaces.Modules;
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
        
        private static readonly string _ConfigFile = "./Data/Resources/config.json";
        private static readonly string _PluginsDatabaseFile = "./Data/Resources/plugins.json";

        private static readonly string _ResourcesFolder = "./Data/Resources";
        private static readonly string _PluginsFolder = "./Data/Plugins";
        private static readonly string _LogsFolder = "./Data/Logs";
        
        public ModuleManager ModuleManager = null!;
        public DiscordBotApplication DiscordBotClient { get; set; } = null!;

        public List<ulong> ServerIDs => ApplicationEnvironmentVariables.GetList("ServerID", new List<ulong>());
        public string PluginDatabase => ApplicationEnvironmentVariables.Get<string>("PluginDatabase", _PluginsDatabaseFile);
        public CustomSettingsDictionary ApplicationEnvironmentVariables { get; private set; } = null!;
        public InternalActionManager InternalActionManager { get; private set; } = null!;
        public PluginManager PluginManager { get; private set; } = null!;

        /// <summary>
        /// Create the application. This method is used to initialize the application. Can not initialize multiple times.
        /// </summary>
        /// <param name="moduleRequirementsSolver">A function that will be called when a module is required to be installed. If set to default, will use the built in method(console)</param>
        public static async Task CreateApplication(Func<ModuleRequirement, Task>? moduleRequirementsSolver)
        {
            if (!await OnlineFunctions.IsInternetConnected())
            {
                Console.WriteLine("No internet connection detected. Exiting ...");
                Environment.Exit(0);
            }
            
            if (CurrentApplication is not null)
            {
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

            CurrentApplication.ModuleManager = new ModuleManager(ModuleRepository.SolveRepo());
            await CurrentApplication.ModuleManager.LoadModules();
            var requirements = await CurrentApplication.ModuleManager.CheckRequiredModules();
            if(requirements.RequireAny)
            {
                moduleRequirementsSolver ??= requirement => CurrentApplication.ModuleManager.SolveRequirementIssues(requirement);
                await moduleRequirementsSolver(requirements);
                
                await CurrentApplication.ModuleManager.LoadModules();
            }
            
            Logger._LoggerModule = CurrentApplication.ModuleManager.GetLoadedModuleWithTag(ModuleType.Logger);
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
        }
        
        public static async Task InvokeMethod(string moduleName, string methodFriendlyName, params object[] parameters)
        {
            var    module     = CurrentApplication.ModuleManager.GetModule(moduleName);
            var methodName = module.Value.MethodMapping[methodFriendlyName];
            
            await CurrentApplication.ModuleManager.InvokeMethod(module.Value, methodName, parameters);
        }
        
        public static async Task<object?> InvokeMethodWithReturnValue(string moduleName, string methodFriendlyName, params object[] parameters)
        {
            var module = CurrentApplication.ModuleManager.GetModule(moduleName).Value;
            var methodName = module.MethodMapping[methodFriendlyName];
            
            var response = await CurrentApplication.ModuleManager.InvokeMethodWithReturnValue(module, methodName, parameters);
            return response;
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
        /// If your custom logger does not have the methods from above, the application might crash.
        /// Please refer to the official logger documentation for more information.
        /// </summary>
        public static class Logger
        {
            internal static LoadedModule _LoggerModule = null!; // initial is null, will be populated when the application will load all modules !!
            public static async void LogException(Exception ex, object sender, bool fullStackTrace = false)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["BaseLogException"], [ex, sender, fullStackTrace]);
            }

            public static async void Log(string message)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["BaseLog"], [message]);
            }

            public static async void Log(string message, LogType logType, string format)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithTypeAndFormat"], [message, logType, format]);
            }

            public static async void Log(string message, LogType logType)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithType"], [message, logType]);
            }

            public static async void Log(string message, object sender)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithSender"], [message, sender]);
            }

            public static async void Log(string message, object sender, LogType type)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["LogWithTypeAndSender"], [message, sender, type]);
            }

            public static async void SetOutFunction(Action<string, LogType> outFunction)
            {
                await CurrentApplication.ModuleManager.InvokeMethod(_LoggerModule.Value, _LoggerModule.Value.MethodMapping["SetPrintFunction"], [outFunction]);
            }
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
