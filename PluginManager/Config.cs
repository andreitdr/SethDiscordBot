using System;
using System.Threading.Tasks;

using PluginManager.Online.Helpers;

namespace PluginManager;

public static class Config
{
    private static bool IsLoaded = false;
    public static async Task Initialize()
    {
        if (IsLoaded)
            return;

        if (!await Settings.sqlDatabase.TableExistsAsync("Plugins"))
            await Settings.sqlDatabase.CreateTableAsync("Plugins", "PluginName", "Version");
        if (!await Settings.sqlDatabase.TableExistsAsync("Variables"))
            await Settings.sqlDatabase.CreateTableAsync("Variables", "VarName", "Value", "ReadOnly");

        IsLoaded = true;
    }

    public static class Variables
    {
        public static async Task<string> GetValueAsync(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            return await Settings.sqlDatabase.GetValueAsync("Variables", "VarName", VarName, "Value");
        }

        public static string GetValue(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            return Settings.sqlDatabase.GetValue("Variables", "VarName", VarName, "Value");
        }


        public static async Task SetValueAsync(string VarName, string Value)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");

            if (await IsReadOnlyAsync(VarName))
                throw new Exception($"Variable ({VarName}) is read only and can not be changed to {Value}");

            await Settings.sqlDatabase.SetValueAsync("Variables", "VarName", VarName, "Value", Value);
        }

        public static void SetValue(string VarName, string Value)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            if (IsReadOnly(VarName))
                throw new Exception($"Variable ({VarName}) is read only and can not be changed to {Value}");
            Settings.sqlDatabase.SetValue("Variables", "VarName", VarName, "Value", Value);
        }


        public static async Task<bool> IsReadOnlyAsync(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            return (await Settings.sqlDatabase.GetValueAsync("Variables", "VarName", VarName, "ReadOnly")).Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        public static bool IsReadOnly(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            return (Settings.sqlDatabase.GetValue("Variables", "VarName", VarName, "ReadOnly")).Equals("true", StringComparison.CurrentCultureIgnoreCase);
        }

        public static async Task SetReadOnlyAsync(string VarName, bool ReadOnly)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            await Settings.sqlDatabase.SetValueAsync("Variables", "VarName", VarName, "ReadOnly", ReadOnly ? "true" : "false");
        }

        public static void SetReadOnly(string VarName, bool ReadOnly)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            Settings.sqlDatabase.SetValue("Variables", "VarName", VarName, "ReadOnly", ReadOnly ? "true" : "false");
        }

        public static async Task<bool> ExistsAsync(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            return await Settings.sqlDatabase.KeyExistsAsync("Variables", "VarName", VarName);
        }

        public static bool Exists(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            return Settings.sqlDatabase.KeyExists("Variables", "VarName", VarName);
        }

        public static async Task AddAsync(string VarName, string Value, bool ReadOnly = false)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            if (await ExistsAsync(VarName))
            {
                await SetValueAsync(VarName, Value);
                await SetReadOnlyAsync(VarName, ReadOnly);
                return;
            }
            await Settings.sqlDatabase.InsertAsync("Variables", VarName, Value, ReadOnly ? "true" : "false");
        }

        public static void Add(string VarName, string Value, bool ReadOnly = false)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            if (Exists(VarName))
            {
                SetValue(VarName, Value);
                SetReadOnly(VarName, ReadOnly);
                return;
            }
            Settings.sqlDatabase.Insert("Variables", VarName, Value, ReadOnly ? "true" : "false");
        }

        public static async Task RemoveKeyAsync(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            await Settings.sqlDatabase.RemoveKeyAsync("Variables", "VarName", VarName);
        }

        public static void RemoveKey(string VarName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded");
            Settings.sqlDatabase.RemoveKey("Variables", "VarName", VarName);
        }
    }

    public static class Plugins
    {
        public static async Task<string> GetVersionAsync(string pluginName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded yet");

            string result = await Settings.sqlDatabase.GetValueAsync("Plugins", "PluginName", pluginName, "Version");
            if (result is null)
                return "0.0.0";

            return result;
        }

        public static string GetVersion(string pluginName)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded yet");

            string result = Settings.sqlDatabase.GetValue("Plugins", "PluginName", pluginName, "Version");
            if (result is null)
                return "0.0.0";

            return result;
        }

        public static async Task SetVersionAsync(string pluginName, VersionString version)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded yet");

            if (!await Settings.sqlDatabase.KeyExistsAsync("Plugins", "PluginName", pluginName))
            {
                await Settings.sqlDatabase.InsertAsync("Plugins", pluginName, version.ToShortString());
                return;
            }
            await Settings.sqlDatabase.SetValueAsync("Plugins", "PluginName", pluginName, "Version", version.ToShortString());
        }

        public static void SetVersion(string pluginName, VersionString version)
        {
            if (!IsLoaded)
                throw new Exception("Config is not loaded yet");

            if (!Settings.sqlDatabase.KeyExists("Plugins", "PluginName", pluginName))
            {
                Settings.sqlDatabase.Insert("Plugins", pluginName, version.ToShortString());
                return;
            }

            Settings.sqlDatabase.SetValue("Plugins", "PluginName", pluginName, "Version", version.ToShortString());
        }

    }
}