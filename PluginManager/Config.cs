using System;
using PluginManager.Others;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace PluginManager
{
    internal class AppConfig
    {
        public Dictionary<string, object>? ApplicationVariables { get; set; }
        public List<string>?               ProtectedKeyWords    { get; set; }
    }

    public static class Config
    {
        public static class PluginConfig
        {
            public static List<Tuple<string, PluginType>> InstalledPlugins = new();

            public static void Load()
            {
                new Thread(LoadCommands).Start();
                new Thread(LoadEvents).Start();
            }

            private static void LoadCommands()
            {
                string   cmd_path = "./Data/Plugins/Commands/";
                string[] files    = Directory.GetFiles(cmd_path, $"*.{Loaders.PluginLoader.pluginCMDExtension}", SearchOption.AllDirectories);
                foreach (var file in files)
                    if (!file.Contains("PluginManager", StringComparison.InvariantCultureIgnoreCase))
                    {
                        string PluginName = new FileInfo(file).Name;
                        string name       = PluginName.Substring(0, PluginName.Length - 1 - PluginManager.Loaders.PluginLoader.pluginCMDExtension.Length);
                        InstalledPlugins.Add(new(name, PluginType.Command));
                    }
            }

            private static void LoadEvents()
            {
                string   eve_path = "./Data/Plugins/Events/";
                string[] files    = Directory.GetFiles(eve_path, $"*.{Loaders.PluginLoader.pluginEVEExtension}", SearchOption.AllDirectories);
                foreach (var file in files)
                    if (!file.Contains("PluginManager", StringComparison.InvariantCultureIgnoreCase))
                        if (!file.Contains("PluginManager", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string PluginName = new FileInfo(file).Name;
                            string name       = PluginName.Substring(0, PluginName.Length - 1 - PluginManager.Loaders.PluginLoader.pluginEVEExtension.Length);
                            InstalledPlugins.Add(new(name, PluginType.Event));
                        }
            }

            public static bool Contains(string pluginName)
            {
                foreach (var tuple in InstalledPlugins)
                {
                    if (tuple.Item1 == pluginName) return true;
                }

                return false;
            }

            public static PluginType GetPluginType(string pluginName)
            {
                foreach (var tuple in InstalledPlugins)
                {
                    if (tuple.Item1 == pluginName) return tuple.Item2;
                }

                return PluginType.Unknown;
            }
        }

        private static AppConfig? appConfig { get; set; }

        public static bool AddValueToVariables<T>(string key, T value, bool isProtected)
        {
            if (appConfig!.ApplicationVariables!.ContainsKey(key)) return false;
            if (value == null) return false;
            appConfig.ApplicationVariables.Add(key, value);
            if (isProtected && key != "Version") appConfig.ProtectedKeyWords!.Add(key);

            SaveConfig();
            return true;
        }

        public static void GetAndAddValueToVariable(string key, string value, bool isReadOnly)
        {
            if (Config.ContainsKey(key)) return;
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

        public static bool SetValue<T>(string key, T value)
        {
            if (!appConfig!.ApplicationVariables!.ContainsKey(key)) return false;
            if (appConfig.ProtectedKeyWords!.Contains(key)) return false;
            if (value == null) return false;

            appConfig.ApplicationVariables[key] = JsonSerializer.SerializeToElement(value);
            SaveConfig();
            return true;
        }

        public static bool RemoveKey(string key)
        {
            if (key == "Version" || key == "token" || key == "prefix") return false;
            appConfig!.ApplicationVariables!.Remove(key);
            appConfig.ProtectedKeyWords!.Remove(key);
            SaveConfig();
            return true;
        }

        public static async void SaveConfig()
        {
            string path = Functions.dataFolder + "config.json";
            await Functions.SaveToJsonFile<AppConfig>(path, appConfig!);
        }

        public static async Task LoadConfig()
        {
            string path = Functions.dataFolder + "config.json";
            if (File.Exists(path))
            {
                appConfig = await Functions.ConvertFromJson<AppConfig>(path);
                Functions.WriteLogFile($"Loaded {appConfig.ApplicationVariables!.Keys.Count} application variables.\nLoaded {appConfig.ProtectedKeyWords!.Count} readonly variables.");
            }
            else
                appConfig = new() { ApplicationVariables = new Dictionary<string, object>(), ProtectedKeyWords = new List<string>() };
        }

        public static bool ContainsValue<T>(T value) => appConfig!.ApplicationVariables!.ContainsValue(value!);
        public static bool ContainsKey(string key)   => appConfig!.ApplicationVariables!.ContainsKey(key);

        public static Dictionary<string, object> GetAllVariables() => new(appConfig!.ApplicationVariables!);
    }
}
