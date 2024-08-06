using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using DiscordBotCore.Interfaces.Logger;
using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Loaders;
using DiscordBotCore.Others.Exceptions;

namespace DiscordBotCore.Modules
{
    internal class ModuleManager
    {
        private static readonly string _BaseModuleFolder = "./Data/Modules";
        
        private readonly string _ModuleFolder;
        internal Dictionary<Type, List<object>> LoadedModules { get; }
        
        public ModuleManager(string moduleFolder)
        {
            _ModuleFolder = moduleFolder;
            LoadedModules = new Dictionary<Type, List<object>>();
        }
        
        public ModuleManager()
        {
            _ModuleFolder = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ModuleFolder", _BaseModuleFolder);
            LoadedModules = new Dictionary<Type, List<object>>();
        }

        public T GetModule<T>() where T : IBaseModule
        {
            if(!LoadedModules.ContainsKey(typeof(T)))
                throw new ModuleNotFoundException<T>();

            if (!LoadedModules[typeof(T)].Any())
                throw new ModuleNotFoundException<T>();

            IModule<T> module = (IModule<T>)LoadedModules[typeof(T)][0];
            return module.Module;
        }

        public async Task LoadModules()
        {
            ModuleLoader loader = new ModuleLoader(_ModuleFolder);
            await loader.LoadFileModules();

            
            var loggers = await loader.LoadModules<ILogger>();
            foreach (var logger in loggers)
            {
                await logger.Initialize();
                Console.WriteLine("Module Loaded: " + logger.Name);
            }

            LoadedModules.Add(typeof(ILogger), loggers.Cast<object>().ToList());

        }


    }
}
