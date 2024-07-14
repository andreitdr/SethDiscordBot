using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

using DiscordBotCore.Interfaces.Modules;
using System.Reflection;

namespace DiscordBotCore.Loaders
{
    internal class ModuleLoader
    {
        private string _moduleFolder;

        public ModuleLoader(string moduleFolder)
        {
            _moduleFolder = moduleFolder;
        }

        public Task LoadFileModules()
        {
            var files = Directory.GetFiles(_moduleFolder, "*.dll");
            foreach (var file in files)
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

        public Task<List<IModule<T>>> LoadModules<T>() where T : IBaseModule
        {
            var moduleType = typeof(IModule<T>);
            var moduleTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => moduleType.IsAssignableFrom(p) && !p.IsInterface);

            var modules = new List<IModule<T>>();
            foreach (var module in moduleTypes)
            {
                try
                {
                    var instance = (IModule<T>?)Activator.CreateInstance(module);
                    if (instance == null)
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
