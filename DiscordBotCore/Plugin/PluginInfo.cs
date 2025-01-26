using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using DiscordBotCore.Online.Helpers;

namespace DiscordBotCore.Plugin;

public class PluginInfo
{
    public string PluginName { get; private set; }
    public string PluginVersion { get; private set; }
    public string FilePath { get; private set; }
    public Dictionary<string, string> ListOfExecutableDependencies {get; private set;}
    public bool IsMarkedToUninstall {get; internal set;}
    public bool IsOfflineAdded { get; internal set; }
    public bool IsEnabled { get; internal set; }

    [JsonConstructor]
    public PluginInfo(string pluginName, string pluginVersion, Dictionary<string, string> listOfExecutableDependencies, bool isMarkedToUninstall, bool isOfflineAdded, bool isEnabled)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        IsMarkedToUninstall = isMarkedToUninstall;
        FilePath = $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{pluginName}.dll";
        IsOfflineAdded = isOfflineAdded;
        IsEnabled = isEnabled;
    }

    public PluginInfo(string pluginName, string pluginVersion, Dictionary<string, string> listOfExecutableDependencies)
    {
        PluginName                   = pluginName;
        PluginVersion                = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        IsMarkedToUninstall          = false;
        FilePath                     = $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{pluginName}.dll";
        IsOfflineAdded               = false;
        IsEnabled                    = true;
    }

    public static PluginInfo FromOnlineInfo(OnlinePlugin plugin, List<OnlineDependencyInfo> dependencies)
    {
        PluginInfo pluginInfo = new PluginInfo(
            plugin.PluginName, plugin.LatestVersion,
            dependencies.Where(dependency => dependency.IsExecutable)
                .ToDictionary(dependency => dependency.DependencyName, dependency => dependency.DownloadLocation)
        );
        
        return pluginInfo;
    }
}
