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
        private string _moduleFolder;
        internal Dictionary<Type, List<object>> LoadedModules { get; private set; }

        public ModuleManager(string moduleFolder)
        {
            _moduleFolder = moduleFolder;
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
            ModuleLoader loader = new ModuleLoader(_moduleFolder);
            await loader.LoadFileModules();


            // Load All Loggers
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
