using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DiscordBotCore.Online.Helpers;

namespace DiscordBotCore.Plugin;

public class PluginInfo
{
    public string PluginName { get; private set; }
    public PluginVersion PluginVersion { get; private set; }
    public string FilePath { get; private set; }
    public Dictionary<string, string> ListOfExecutableDependencies {get; private set;}
    public bool IsMarkedToUninstall {get; internal set;}
    public bool IsOfflineAdded { get; internal set; }
    public bool IsEnabled { get; internal set; }

    [JsonConstructor]
    public PluginInfo(string pluginName, PluginVersion pluginVersion, Dictionary<string, string> listOfExecutableDependencies, bool isMarkedToUninstall, bool isOfflineAdded, bool isEnabled)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        IsMarkedToUninstall = isMarkedToUninstall;
        FilePath = $"{Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"]}/{pluginName}.dll";
        IsOfflineAdded = isOfflineAdded;
        IsEnabled = isEnabled;
    }

    public PluginInfo(string pluginName, PluginVersion pluginVersion, Dictionary<string, string> listOfExecutableDependencies)
    {
        PluginName    = pluginName;
        PluginVersion = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        IsMarkedToUninstall = false;
        FilePath = $"{Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"]}/{pluginName}.dll";
        IsOfflineAdded = false;
        IsEnabled = true;
    }

    public static PluginInfo FromOnlineInfo(PluginOnlineInfo onlineInfo)
    {
        return new PluginInfo(onlineInfo.Name,
            onlineInfo.Version,
            onlineInfo.Dependencies
                .Where(dep => dep.IsExecutable)
                .Select(dep => new KeyValuePair<string, string>(dep.DependencyName, dep.DownloadLocation))
                .ToDictionary());
    }
}
