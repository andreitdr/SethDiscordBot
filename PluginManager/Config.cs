using System;
using PluginManager.Others;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;

namespace PluginManager
{
    internal class AppConfig
    {
        public Dictionary<string, object>? ApplicationVariables { get; init; }
        public List<string>? ProtectedKeyWords { get; init; }
        public Dictionary<string, string>? PluginVersions { get; init; }
    }

    public static class Config
    {
        public static class PluginConfig
        {
            public static readonly List<Tuple<string, PluginType>> InstalledPlugins = new();

            public static void Load()
            {
                new Thread(LoadCommands).Start();
                new Thread(LoadEvents).Start();
            }

            private static void LoadCommands()
            {
                string cmd_path = "./Data/Plugins/Commands/";
                string[] files = Directory.GetFiles(cmd_path, $"*.{Loaders.PluginLoader.pluginCMDExtension}", SearchOption.AllDirectories);
                foreach (var file in files)
                    if (!file.Contains("PluginManager", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string PluginName = new FileInfo(file).Name;
                        string name = PluginName.Substring(0, PluginName.Length - 1 - PluginManager.Loaders.PluginLoader.pluginCMDExtension.Length);
                        InstalledPlugins.Add(new(name, PluginType.Command));
                    }
            }

            private static void LoadEvents()
            {
                string eve_path = "./Data/Plugins/Events/";
                string[] files = Directory.GetFiles(eve_path, $"*.{Loaders.PluginLoader.pluginEVEExtension}", SearchOption.AllDirectories);
                foreach (var file in files)
                    if (!file.Contains("PluginManager", StringComparison.InvariantCultureIgnoreCase))
                        if (!file.Contains("PluginManager", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string PluginName = new FileInfo(file).Name;
                            string name = PluginName.Substring(0, PluginName.Length - 1 - PluginManager.Loaders.PluginLoader.pluginEVEExtension.Length);
                            InstalledPlugins.Add(new(name, PluginType.Event));
                        }
            }

            public static bool Contains(string pluginName)
            {
                foreach (var tuple in InstalledPlugins)
                    if (tuple.Item1 == pluginName)
                        return true;

                return false;
            }

            public static PluginType GetPluginType(string pluginName)
            {
                foreach (var tuple in InstalledPlugins)
                    if (tuple.Item1 == pluginName)
                        return tuple.Item2;


                return PluginType.Unknown;
            }
        }

        private static AppConfig? appConfig { get; set; }

        public static string GetPluginVersion(string pluginName) => appConfig!.PluginVersions![pluginName];
        public static void SetPluginVersion(string pluginName, string newVersion)
        {
            if (appConfig!.PluginVersions!.ContainsKey(pluginName))
                appConfig.PluginVersions[pluginName] = newVersion;
            else appConfig.PluginVersions.Add(pluginName, newVersion);

            // SaveConfig();
        }

        public static void RemovePluginVersion(string pluginName) => appConfig!.PluginVersions!.Remove(pluginName);
        public static bool PluginVersionsContainsKey(string pluginName) => appConfig!.PluginVersions!.ContainsKey(pluginName);

        public static void AddValueToVariables<T>(string key, T value, bool isProtected)
        {
            if (value == null)
                throw new Exception("The value cannot be null");
            if (appConfig!.ApplicationVariables!.ContainsKey(key))
                throw new Exception($"The key ({key}) already exists in the variables. Value {GetValue<T>(key)}");

            appConfig.ApplicationVariables.Add(key, value);
            if (isProtected && key != "Version")
                appConfig.ProtectedKeyWords!.Add(key);

            SaveConfig(SaveType.NORMAL);
        }

        public static Type GetVariableType(string value)
        {
            if (int.TryParse(value, out var intValue))
                return typeof(int);
            if (bool.TryParse(value, out var boolValue))
                return typeof(bool);
            if (float.TryParse(value, out var floatValue))
                return typeof(float);
            if (double.TryParse(value, out var doubleValue))
                return typeof(double);
            if (uint.TryParse(value, out var uintValue))
                return typeof(uint);
            if (long.TryParse(value, out var longValue))
                return typeof(long);
            if (byte.TryParse(value, out var byteValue))
                return typeof(byte);
            return typeof(string);
        }

        public static void GetAndAddValueToVariable(string key, string value, bool isReadOnly)
        {
            if (Config.ContainsKey(key))
                return;
            if (int.TryParse(value, out var intValue))
                Config.AddValueToVariables(key, intValue, isReadOnly);
            else if (bool.TryParse(value, out var boolValue))
                Config.AddValueToVariables(key, boolValue, isReadOnly);
            else if (float.TryParse(value, out var floatValue))
                Config.AddValueToVariables(key, floatValue, isReadOnly);
            else if (double.TryParse(value, out var doubleValue))
                Config.AddValueToVariables(key, doubleValue, isReadOnly);
            else if (uint.TryParse(value, out var uintValue))
                Config.AddValueToVariables(key, uintValue, isReadOnly);
            else if (long.TryParse(value, out var longValue))
                Config.AddValueToVariables(key, longValue, isReadOnly);
            else if (byte.TryParse(value, out var byteValue))
                Config.AddValueToVariables(key, byteValue, isReadOnly);
            else
                Config.AddValueToVariables(key, value, isReadOnly);
        }

        public static T? GetValue<T>(string key)
        {
            if (!appConfig!.ApplicationVariables!.ContainsKey(key)) return default;
            try
            {
                JsonElement element = (JsonElement)appConfig.ApplicationVariables[key];
                return element.Deserialize<T>();
            }
            catch
            {
                return (T)appConfig.ApplicationVariables[key];
            }
        }

        public static void SetValue<T>(string key, T value)
        {
            if (value == null)
                throw new Exception("Value is null");
            if (!appConfig!.ApplicationVariables!.ContainsKey(key))
                throw new Exception("Key does not exist in the config file");
            if (appConfig.ProtectedKeyWords!.Contains(key))
                throw new Exception("Key is protected");

            appConfig.ApplicationVariables[key] = JsonSerializer.SerializeToElement(value);
            SaveConfig(SaveType.NORMAL);
        }

        public static void RemoveKey(string key)
        {
            if (key == "Version" || key == "token" || key == "prefix")
                throw new Exception("Key is protected");
            appConfig!.ApplicationVariables!.Remove(key);
            appConfig.ProtectedKeyWords!.Remove(key);
            SaveConfig(SaveType.NORMAL);
        }

        public static async Task SaveConfig(SaveType type)
        {
            if (type == SaveType.NORMAL)
            {
                string path = Functions.dataFolder + "config.json";
                await Functions.SaveToJsonFile<AppConfig>(path, appConfig!);
                return;
            }
            if (type == SaveType.BACKUP)
            {
                string path = Functions.dataFolder + "config.json.bak";
                await Functions.SaveToJsonFile<AppConfig>(path, appConfig!);
                return;
            }

        }

        public static async Task LoadConfig()
        {
            string path = Functions.dataFolder + "config.json";
            if (File.Exists(path))
            {
                try
                {
                    appConfig = await Functions.ConvertFromJson<AppConfig>(path);
                }
                catch (Exception ex)
                {
                    File.Delete(path);
                    Console.WriteLine("An error occured while loading the settings. Importing from backup file...");
                    path = Functions.dataFolder + "config.json.bak";
                    appConfig = await Functions.ConvertFromJson<AppConfig>(path);
                    Functions.WriteErrFile(ex.Message);
                }


                Functions.WriteLogFile($"Loaded {appConfig.ApplicationVariables!.Keys.Count} application variables.\nLoaded {appConfig.ProtectedKeyWords!.Count} readonly variables.");
                return;
            }
            appConfig = new() { ApplicationVariables = new Dictionary<string, object>(), ProtectedKeyWords = new List<string>(), PluginVersions = new Dictionary<string, string>() };
        }

        public static bool ContainsValue<T>(T value) => appConfig!.ApplicationVariables!.ContainsValue(value!);
        public static bool ContainsKey(string key) => appConfig!.ApplicationVariables!.ContainsKey(key);

        public static ReadOnlyDictionary<string, object> GetAllVariables() => new(appConfig!.ApplicationVariables!);
    }
}
