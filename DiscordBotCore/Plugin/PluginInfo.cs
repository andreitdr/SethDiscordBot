using System;
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
    public Dictionary<string, string> ListOfDependancies {get; private set;}
    public bool IsMarkedToUninstall {get; internal set;}
    public bool IsOfflineAdded { get; internal set; }

    [JsonConstructor]
    public PluginInfo(string pluginName, PluginVersion pluginVersion, Dictionary<string, string> listOfDependancies, bool isMarkedToUninstall, bool isOfflineAdded)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        ListOfDependancies = listOfDependancies;
        IsMarkedToUninstall = isMarkedToUninstall;
        FilePath = $"{Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"]}/{pluginName}.dll";
        IsOfflineAdded = isOfflineAdded;
    }

    public PluginInfo(string pluginName, PluginVersion pluginVersion, Dictionary<string, string> listOfDependancies)
    {
        PluginName    = pluginName;
        PluginVersion = pluginVersion;
        ListOfDependancies = listOfDependancies;
        IsMarkedToUninstall = false;
        FilePath = $"{Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"]}/{pluginName}.dll";
    }

    public static PluginInfo FromOnlineInfo(PluginOnlineInfo onlineInfo)
    {
        return new PluginInfo(onlineInfo.Name, onlineInfo.Version, onlineInfo.Dependencies.Select(dep => new KeyValuePair<string, string>(dep.DependencyName, dep.DownloadLocation)).ToDictionary());
    }
}
