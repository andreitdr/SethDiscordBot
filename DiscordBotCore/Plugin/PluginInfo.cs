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
        FilePath = $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{pluginName}.dll";
        IsOfflineAdded = isOfflineAdded;
        IsEnabled = isEnabled;
    }

    public PluginInfo(string pluginName, PluginVersion pluginVersion, Dictionary<string, string> listOfExecutableDependencies)
    {
        PluginName                   = pluginName;
        PluginVersion                = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        IsMarkedToUninstall          = false;
        FilePath                     = $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{pluginName}.dll";
        IsOfflineAdded               = false;
        IsEnabled                    = true;
    }

    public static PluginInfo FromOnlineInfo(PluginOnlineInfo onlineInfo)
    {
        var pluginName             = onlineInfo.Name;
        var version                = onlineInfo.Version;
        var dependencies= onlineInfo.Dependencies;
        
        if(dependencies is null)
        {
            return new PluginInfo(pluginName, version, new Dictionary<string, string>());
        }
        
        var executableDependencies = dependencies.Where(dep => dep.IsExecutable);
        var dictDependencies       = executableDependencies.Select(dep => new KeyValuePair<string, string>(dep.DependencyName, dep.DownloadLocation)).ToDictionary();

        return new PluginInfo(pluginName, version, dictDependencies);
        
    }
}
