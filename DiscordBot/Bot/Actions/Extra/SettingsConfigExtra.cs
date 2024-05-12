using System.Linq;
using DiscordBotCore;

namespace DiscordBot.Bot.Actions.Extra;

internal static class SettingsConfigExtra
{

    internal static void SetSettings(string key, params string[] value)
    {
        if (key is null) return;

        if (value is null) return;

        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey(key))
            return;

        Application.CurrentApplication.ApplicationEnvironmentVariables[key] = string.Join(' ', value);
        // Config.Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile().Wait();
    }

    internal static void RemoveSettings(string key)
    {
        if (key is null) return;

        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey(key))
            return;

        Application.CurrentApplication.ApplicationEnvironmentVariables.Remove(key);
    }

    internal static void AddSettings(string key, params string[] value)
    {
        if (key is null) return;

        if (value is null) return;

        if (Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey(key))
            return;

        Application.CurrentApplication.ApplicationEnvironmentVariables.Add(key, string.Join(' ', value));
        // Config.Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile().Wait();
    }
}
