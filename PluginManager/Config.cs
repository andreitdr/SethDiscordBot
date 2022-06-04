using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using PluginManager.Others;

namespace PluginManager
{
    public static class Config
    {
        private static readonly Dictionary<string, string> ApplicationVariables = new();
        private static readonly List<string>               ConstantTokens       = new() { "token" };

        public static void AppendToDictionary(Dictionary<string, string> dictionary)
        {
            foreach (var kvp in dictionary) ApplicationVariables.TryAdd(kvp.Key, kvp.Value);
        }

        public static bool AddValueToVariables(string key, string value, bool constant)
        {
            bool req = AddValueToVariables(key, value);
            if (constant) ConstantTokens.Add(key);

            return req;
        }

        public static bool AddValueToVariables(string key, string value)
        {
            if (ApplicationVariables.ContainsKey(key))
            {
                return false;
            }

            ApplicationVariables.Add(key, value);
            return true;
        }

        public static string? GetValue(string key)
        {
            if (!ApplicationVariables.ContainsKey(key))
            {
                if (key != "token") Console.WriteLine("The key is not present in the dictionary");
                return null;
            }

            return ApplicationVariables[key];
        }

        public static bool SetValue(string key, string value)
        {
            if (!ApplicationVariables.ContainsKey(key)) return false;
            if (ConstantTokens.Contains(key)) return false;
            ApplicationVariables[key] = value;
            return true;
        }

        public static bool RemoveKey(string key)
        {
            if (ConstantTokens.Contains(key)) return false;


            ApplicationVariables.Remove(key);
            return true;
        }

        public static async void SaveDictionary()
        {
            string path = Functions.dataFolder + "var.dat";
            await Functions.SaveToJsonFile(path, ApplicationVariables);
        }

        public static async void LoadDictionary()
        {
            string path = Functions.dataFolder + "var.dat";
            var    d    = await Functions.ConvertFromJson<Dictionary<string, string>>(path);
            ApplicationVariables.Clear();
            AppendToDictionary(d);
        }

        public static string GetKey(string        value) => ApplicationVariables.Keys.FirstOrDefault(x => ApplicationVariables[x] == value);
        public static bool   ContainsValue(string value) => ApplicationVariables.ContainsValue(value);
        public static bool   ContainsKey(string   key)   => ApplicationVariables.ContainsKey(key);
    }
}
