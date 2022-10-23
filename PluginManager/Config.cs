using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

using PluginManager.Others;

namespace PluginManager;

internal class AppConfig
{
    public string? UpdaterVersion { get; set; }
    public Dictionary<string, object>? ApplicationVariables { get; init; }
    public List<string>? ProtectedKeyWords { get; init; }
    public Dictionary<string, string>? PluginVersions { get; init; }
}

public static class Config
{
    private static AppConfig? appConfig { get; set; }

    public static string UpdaterVersion
    {
        get => appConfig!.UpdaterVersion!;
        set => appConfig!.UpdaterVersion = value;
    }

    public static string GetPluginVersion(string pluginName)
    {
        return appConfig!.PluginVersions![pluginName];
    }

    public static void SetPluginVersion(string pluginName, string newVersion)
    {
        if (appConfig!.PluginVersions!.ContainsKey(pluginName))
            appConfig.PluginVersions[pluginName] = newVersion;
        else appConfig.PluginVersions.Add(pluginName, newVersion);

        // SaveConfig();
    }

    public static void RemovePluginVersion(string pluginName)
    {
        appConfig!.PluginVersions!.Remove(pluginName);
    }

    public static bool PluginVersionsContainsKey(string pluginName)
    {
        return appConfig!.PluginVersions!.ContainsKey(pluginName);
    }

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
        if (ContainsKey(key))
            return;
        if (int.TryParse(value, out var intValue))
            AddValueToVariables(key, intValue, isReadOnly);
        else if (bool.TryParse(value, out var boolValue))
            AddValueToVariables(key, boolValue, isReadOnly);
        else if (float.TryParse(value, out var floatValue))
            AddValueToVariables(key, floatValue, isReadOnly);
        else if (double.TryParse(value, out var doubleValue))
            AddValueToVariables(key, doubleValue, isReadOnly);
        else if (uint.TryParse(value, out var uintValue))
            AddValueToVariables(key, uintValue, isReadOnly);
        else if (long.TryParse(value, out var longValue))
            AddValueToVariables(key, longValue, isReadOnly);
        else if (byte.TryParse(value, out var byteValue))
            AddValueToVariables(key, byteValue, isReadOnly);
        else
            AddValueToVariables(key, value, isReadOnly);
    }

    public static T? GetValue<T>(string key)
    {
        if (!appConfig!.ApplicationVariables!.ContainsKey(key)) return default;
        try
        {
            var element = (JsonElement)appConfig.ApplicationVariables[key];
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

    public static bool TrySetValue<T>(string key, T value)
    {
        if (Config.ContainsKey(key))
        {
            try
            {
                Config.SetValue(key, value);
                return true;
            }
            catch
            {
                return false;
            }
        }

        Config.AddValueToVariables(key, value, false);
        return true;
    }

    public static void RemoveKey(string key)
    {
        if (key == "Version" || key == "token" || key == "prefix")
            throw new Exception("Key is protected");
        appConfig!.ApplicationVariables!.Remove(key);
        appConfig.ProtectedKeyWords!.Remove(key);
        SaveConfig(SaveType.NORMAL);
    }

    public static bool IsReadOnly(string key)
    {
        return appConfig.ProtectedKeyWords.Contains(key);
    }

    public static async Task SaveConfig(SaveType type)
    {
        if (type == SaveType.NORMAL)
        {
            var path = Functions.dataFolder + "config.json";
            await Functions.SaveToJsonFile(path, appConfig!);
            return;
        }

        if (type == SaveType.BACKUP)
        {
            var path = Functions.dataFolder + "config.json.bak";
            await Functions.SaveToJsonFile(path, appConfig!);
        }
    }

    public static async Task LoadConfig()
    {
        var path = Functions.dataFolder + "config.json";
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


            Functions.WriteLogFile(
                $"Loaded {appConfig.ApplicationVariables!.Keys.Count} application variables.\nLoaded {appConfig.ProtectedKeyWords!.Count} readonly variables.");
            return;
        }

        if (File.Exists(Functions.dataFolder + "config.json.bak"))
        {
            try
            {
                Console.WriteLine("An error occured while loading the settings. Importing from backup file...");
                path = Functions.dataFolder + "config.json.bak";
                appConfig = await Functions.ConvertFromJson<AppConfig>(path);

                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        appConfig = new AppConfig
        {
            ApplicationVariables = new Dictionary<string, object>(),
            ProtectedKeyWords = new List<string>(),
            PluginVersions = new Dictionary<string, string>(),
            UpdaterVersion = "-1"
        };
    }

    public static bool ContainsValue<T>(T value)
    {
        return appConfig!.ApplicationVariables!.ContainsValue(value!);
    }

    public static bool ContainsKey(string key)
    {
        return appConfig!.ApplicationVariables!.ContainsKey(key);
    }

    public static IDictionary<string, object>? GetAllVariables()
    {
        return appConfig?.ApplicationVariables;
    }
}