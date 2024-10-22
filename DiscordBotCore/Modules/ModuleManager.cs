using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Loaders;
using DiscordBotCore.Online;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;
using DiscordBotCore.Repository;
using Newtonsoft.Json;

namespace DiscordBotCore.Modules
{
    
    public class LoadedModule
    {
        public IModule    Value { get; init; }
        public ModuleData ModuleData { get; init; }
        public LoadedModule(IModule module, ModuleData moduleData)
        {
            Value = module;
            ModuleData = moduleData;
        }
    }
    
    public class ModuleManager
    {
        private static readonly string _BaseModuleFolder = "./Data/Modules";
        private static readonly string _BaseModuleConfig = "./Data/Resources/modules.json";
        
        // private const string _ModuleDatabase = "https://raw.githubusercontent.com/andreitdr/SethPlugins/tests/modules.json";
        private ModuleRepository _ModuleRepository;
        private List<LoadedModule> Modules { get; }
        
        public IEnumerable<ModuleData> GetLocalModules()
        {
            return Modules.Select(module => module.ModuleData);
        }

        internal ModuleManager(ModuleRepository moduleRepository)
        {
            Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ModuleFolder", _BaseModuleFolder);
            Modules = new();
            _ModuleRepository = moduleRepository;
        }

        public async Task<ModuleOnlineData?> ServerGetModuleWithName(string moduleName)
        {
            var modules = await ServerGetAllModules();
            return modules.FirstOrDefault(module => module?.ModuleName == moduleName, null);
        }

        public async Task<List<ModuleOnlineData>> ServerGetAllModules(ModuleType? moduleTypeFilter = null)
        {
            var jsonDatabaseRemote = await _ModuleRepository.JsonGetAllModules();

            var modules = await JsonManager.ConvertFromJson<List<ModuleOnlineData>>(jsonDatabaseRemote);
            
            if(moduleTypeFilter is not null)
                modules = modules.FindAll(m => m.ModuleType == moduleTypeFilter);
            
            return modules;
        }
        
        public async Task InstallModule(string moduleName, IProgress<float> progressToWrite)
        {
            string? moduleFolder = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ModuleFolder");
            
            if(moduleFolder is null)
                throw new DirectoryNotFoundException("Module folder not found"); // Should never happen
            
            Directory.CreateDirectory(moduleFolder);
            
            List<ModuleOnlineData> modules = await ServerGetAllModules();

            string url = modules.Find(m => m.ModuleName == moduleName)?.ModuleDownloadUrl ?? string.Empty;

            if(string.IsNullOrEmpty(url))
                return;

            string filePath = moduleFolder + "/" + moduleName + ".dll";
            await ServerCom.DownloadFileAsync(url, filePath, progressToWrite);

            ModuleData localModuleData = new ModuleData(moduleName, filePath, true);

            await AddModuleToDatabase(localModuleData);
        }
        
        public LoadedModule GetModule(string moduleName)
        {
            var result = Modules.FirstOrDefault(module => module.ModuleData.ModuleName == moduleName);

            if(result is null)
            {
                throw new ModuleNotFound(moduleName);
            }

            return result;
        }

        public LoadedModule GetLoadedModuleWithTag(ModuleType moduleType)
        {
            var result = Modules.FirstOrDefault(module => module.Value.ModuleType == moduleType);

            if(result is null)
            {
                throw new ModuleNotFound(moduleType);
            }

            return result;
        }

        public async Task AddModuleToDatabase(ModuleData moduleData)
        {
            string moduleConfigPath = Application.CurrentApplication.ApplicationEnvironmentVariables
                                     .Get<string>("ModuleConfig", _BaseModuleConfig);

            List<ModuleData>? listOfModuleData = null;
            if (File.Exists(moduleConfigPath))
            {
                string moduleConfigFile = await File.ReadAllTextAsync(moduleConfigPath);
                listOfModuleData = JsonConvert.DeserializeObject<List<ModuleData>>(moduleConfigFile);
            }
            

            if (listOfModuleData is null)
            {
                listOfModuleData = new List<ModuleData>();
            }

            listOfModuleData.Add(moduleData);

            string json = JsonConvert.SerializeObject(listOfModuleData, Formatting.Indented);
            await File.WriteAllTextAsync(moduleConfigPath, json);
        }

        internal Task<ModuleRequirement> CheckRequiredModules()
        {
            ModuleRequirement moduleRequirement = new ModuleRequirement();
            if (Modules.All(module => module.Value.ModuleType != ModuleType.Logger))
            {
                moduleRequirement.AddType(ModuleType.Logger);
            }
            
            return Task.FromResult(moduleRequirement);
        }

        internal async Task SolveRequirementIssues(ModuleRequirement requirements)
        {
            if (!requirements.RequireAny)
            {
                return;
            }

            foreach (var module in requirements.RequiredModulesWithTypes)
            {
                var availableModules = await ServerGetAllModules(module);

                Console.WriteLine("Please select a module of type " + module);
                for (var i = 0; i < availableModules.Count; i++)
                {
                    Console.WriteLine((i+1) + " - " + availableModules[i].ModuleName);
                    Console.WriteLine("Author: " + availableModules[i].ModuleAuthor);
                    Console.WriteLine("Description: " + availableModules[i].ModuleDescription);

                    Console.WriteLine();
                }

                Console.WriteLine("Please select a module by typing the number:");
                int selectedModule = int.Parse(Console.ReadLine() ?? string.Empty);

                if (selectedModule < 1 || selectedModule > availableModules.Count)
                {
                    Console.WriteLine("Invalid module selected");
                    Environment.Exit(-1);
                }

                IProgress<float> progress = new Progress<float>(f => Console.Write($"\b{f}"));
                await InstallModule(availableModules[selectedModule - 1].ModuleName, progress);
            }


            Console.WriteLine("All required modules installed. Please restart the application");
            System.Diagnostics.Process.Start(System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName);
        }

        internal async Task LoadModules()
        {
            Modules.Clear();
            
            string moduleConfigPath = Application.CurrentApplication.ApplicationEnvironmentVariables
                                                 .Get<string>("ModuleConfig", _BaseModuleConfig);
            
            if(!File.Exists(moduleConfigPath))
                return;

            string moduleConfigFile = await File.ReadAllTextAsync(moduleConfigPath);
            List<ModuleData>? listOfModuleData = JsonConvert.DeserializeObject<List<ModuleData>>(moduleConfigFile);

            if (listOfModuleData is null)
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
                    try{
                        await module.Initialize();
                        Modules.Add(new LoadedModule(module, moduleData));
                    }catch(Exception e){
                        Console.WriteLine($"Error loading module {moduleData.ModuleName}: {e.Message}");
                    }
                }
            }
        }

        internal async Task<object?> InvokeMethodWithReturnValue(string moduleName, string methodName, object[] parameters)
        {
            IModule module = GetModule(moduleName).Value;
            var method = module.GetType().GetMethod(methodName);

            if (method is null)
            {
                throw new ModuleMethodNotFound(module, methodName);
            }

            object? result = await Task.Run(() => method.Invoke(module, parameters));

            return result;
        }

        internal async Task<object?> InvokeMethodWithReturnValue(IModule module, string methodName, object[] parameters)
        {
            var method = module.GetType().GetMethod(methodName);

            if (method is null)
            {
                throw new ModuleMethodNotFound(module, methodName);
            }

            object? result = await Task.Run(() => method.Invoke(module, parameters));

            return result;
        }


        internal async Task InvokeMethod(string moduleName, string methodName, object[] parameters)
        {
            IModule module = GetModule(moduleName).Value;
            var method = module.GetType().GetMethod(methodName);

            if (method is null)
            {
                throw new ModuleMethodNotFound(module, methodName);
            }

            await Task.Run(() => method.Invoke(module, parameters));
        }

        internal async Task InvokeMethod(IModule module, string methodName, object[] parameters)
        {
            var method = module.GetType().GetMethod(methodName);

            if (method is null)
            {
                throw new ModuleMethodNotFound(module, methodName);
            }

            await Task.Run(() => method.Invoke(module, parameters));
        }
    }
}
