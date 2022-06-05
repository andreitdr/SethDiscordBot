using System;
using PluginManager.Others;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace PluginManager
{
    internal class AppConfig
    {
        public Dictionary<string, string> ApplicationVariables { get; set; }
        public List<string>               ProtectedKeyWords    { get; set; }
    }

    public static class Config
    {
        private static AppConfig appConfig = null;

        public static bool AddValueToVariables(string key, string value, bool isReadOnly)
        {
            if (appConfig.ApplicationVariables.ContainsKey(key)) return false;
            appConfig.ApplicationVariables.Add(key, value);
            if (isReadOnly) appConfig.ProtectedKeyWords.Add(key);
            SaveConfig();
            return true;
        }

        public static string? GetValue(string key)
        {
            if (!appConfig.ApplicationVariables.ContainsKey(key)) return null;
            return appConfig.ApplicationVariables[key];
        }

        public static bool SetValue(string key, string value)
        {
            if (!appConfig.ApplicationVariables.ContainsKey(key)) return false;
            if (appConfig.ProtectedKeyWords.Contains(key)) return false;
            appConfig.ApplicationVariables[key] = value;
            SaveConfig();
            return true;
        }

        public static bool RemoveKey(string key)
        {
            appConfig.ApplicationVariables.Remove(key);
            appConfig.ProtectedKeyWords.Remove(key);
            return true;
        }

        public static async void SaveConfig()
        {
            string path = Functions.dataFolder + "var.dat";
            await Functions.SaveToJsonFile<AppConfig>(path, appConfig);
        }

        public static async Task LoadConfig()
        {
            string path = Functions.dataFolder + "var.dat";
            if (File.Exists(path))
            {
                appConfig = await Functions.ConvertFromJson<AppConfig>(path);
                Functions.WriteLogFile($"Loaded {appConfig.ApplicationVariables.Keys.Count} application variables.\nLoaded {appConfig.ProtectedKeyWords.Count} readonly variables.");
                //Console.WriteLine($"Loaded {appConfig.ApplicationVariables.Count} application variables !");
            }
            else
                appConfig = new() { ApplicationVariables = new Dictionary<string, string>(), ProtectedKeyWords = new List<string>() };
        }

        public static string? GetKey(string        value) => appConfig.ApplicationVariables.Keys.FirstOrDefault(x => appConfig.ApplicationVariables[x] == value);
        public static bool    ContainsValue(string value) => appConfig.ApplicationVariables.ContainsValue(value);
        public static bool    ContainsKey(string   key)   => appConfig.ApplicationVariables.ContainsKey(key);

        public static Dictionary<string, string> GetAllVariables() => new Dictionary<string, string>(appConfig.ApplicationVariables);
    }
}
