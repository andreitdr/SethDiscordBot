using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using DiscordBotCore.Interfaces.Modules;
using System.Reflection;
using DiscordBotCore.Modules;

namespace DiscordBotCore.Loaders
{
    internal class ModuleLoader
    {
        private readonly List<ModuleData> _ModuleData;

        public ModuleLoader(List<ModuleData> moduleFolder)
        {
            _ModuleData = moduleFolder;
        }

        public Task LoadFileModules()
        {
            var paths = _ModuleData.Select(module => module.ModulePath);
            
            foreach (var file in paths)
            {
                try
                {
                    Assembly.LoadFrom(file);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error loading module {file}: {e.Message}");
                }
            }

            return Task.CompletedTask;
        }

        public Task<List<IModule>> LoadModules()
        {
            var moduleType = typeof(IModule);
            var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => moduleType.IsAssignableFrom(p) && !p.IsInterface);

            var modules = new List<IModule>();
            foreach (var module in moduleTypes)
            {
                try
                {
                    var instance = (IModule?)Activator.CreateInstance(module);
                    if (instance is null)
                    {
                        Console.WriteLine($"Error loading module {module.Name}: Could not create instance");
                        continue;
                    }

                    modules.Add(instance);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error loading module {module.Name}: {e.Message}");
                }
            }

            return Task.FromResult(modules);
        }
    }
}
