using System.Linq;
using PluginManager;

namespace DiscordBot.Bot.Actions.Extra;

internal static class SettingsConfigExtra
{
    internal static  void SetSettings(string key, params string[] value)
    {
        if (key is null) return;

        if (value is null) return;
        
        if (!Config.AppSettings.ContainsKey(key))
            return;
        
        Config.AppSettings[key] = string.Join(' ', value);
         // Config.AppSettings.SaveToFile().Wait();
    }

    internal static void RemoveSettings(string key)
    {
        if (key is null) return;
        
        if(!Config.AppSettings.ContainsKey(key))
            return;
        
        Config.AppSettings.Remove(key);
    }

    internal static void AddSettings(string key, params string[] value)
    {
        if (key is null) return;

        if (value is null) return;
        
        if (Config.AppSettings.ContainsKey(key))
            return;
        
        Config.AppSettings.Add(key, string.Join(' ', value));
        // Config.AppSettings.SaveToFile().Wait();
    }
}