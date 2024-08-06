using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Loaders;
using Newtonsoft.Json;

namespace DiscordBotCore.Modules
{
    internal class ModuleManager
    {
        private static readonly string _BaseModuleFolder = "./Data/Modules";
        private static readonly string _BaseModuleConfig = "./Data/Resources/modules.json";
        internal Dictionary<ModuleData, IModule> Modules { get; set; }
        
        public ModuleManager()
        {
            Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ModuleFolder", _BaseModuleFolder);
            Modules = new Dictionary<ModuleData, IModule>();
        }

        public KeyValuePair<ModuleData, IModule> GetModule(string moduleName)
        {
            return Modules.FirstOrDefault(module => module.Key.ModuleName == moduleName);
        }
        
        public KeyValuePair<ModuleData, IModule> GetModule(ModuleType moduleType)
        {
            return Modules.First(module => module.Value.ModuleType == moduleType);
        }

        public async Task LoadModules()
        {
            string moduleConfigPath = Application.CurrentApplication.ApplicationEnvironmentVariables
                                                 .Get<string>("ModuleConfig", _BaseModuleConfig);
            
            string           moduleConfigFile = await File.ReadAllTextAsync(moduleConfigPath);
            List<ModuleData>? listOfModuleData = JsonConvert.DeserializeObject<List<ModuleData>>(moduleConfigFile);
            
            if(listOfModuleData is null)
                return;
            
            if (!listOfModuleData.Any())
            {
                return;
            }
            
            ModuleLoader moduleLoader = new ModuleLoader(listOfModuleData);
            await moduleLoader.LoadFileModules();
            var modules = await moduleLoader.LoadModules();
            
            foreach (var module in modules)
            {
                
                ModuleData? moduleData = listOfModuleData.FirstOrDefault(data => data.ModuleName == module.Name);

                if (moduleData is null)
                {
                    continue;
                }
                
                if (moduleData.IsEnabled)
                {
                    await module.Initialize(); // TODO: Add error handling
                    Modules.Add(moduleData, module);
                }
            }
        }

        public async Task InvokeMethod(string moduleName, string methodName, object[] parameters)
        {
            IModule module = GetModule(moduleName).Value;
            var method = module.GetType().GetMethod(methodName);
            
            if (method is null)
            {
                throw new Exception("Method not found"); // TODO: Add custom exception
            }

            await Task.Run(() => method.Invoke(module, parameters));
        }

        public async Task InvokeMethod(IModule module, string methodName, object[] parameters)
        {
            var method = module.GetType().GetMethod(methodName);
            
            if (method is null)
            {
                throw new Exception($"Method not found {methodName}"); 
            }

            await Task.Run(() => method.Invoke(module, parameters));
        }


    }
}
