using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using PluginManager.Online.Helpers;

namespace PluginManager.Plugin;

public class PluginInfo
{
    public string PluginName { get; private set; }
    public PluginVersion PluginVersion { get; private set; }
    public string FilePath { get; private set; }
    public List<string> ListOfDependancies {get; private set;}
    public bool IsMarkedToUninstall {get; internal set;}

    [JsonConstructor]
    public PluginInfo(string pluginName, PluginVersion pluginVersion, List<string> listOfDependancies, bool isMarkedToUninstall)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        ListOfDependancies = listOfDependancies;
        IsMarkedToUninstall = isMarkedToUninstall;
        FilePath = $"{Config.AppSettings["PluginFolder"]}/{pluginName}.dll";
    }

    public PluginInfo(string pluginName, PluginVersion pluginVersion, List<string> listOfDependancies)
    {
        PluginName    = pluginName;
        PluginVersion = pluginVersion;
        ListOfDependancies = listOfDependancies;
        IsMarkedToUninstall = false;
        FilePath = $"{Config.AppSettings["PluginFolder"]}/{pluginName}.dll";
    }

    public static PluginInfo FromOnlineInfo(PluginOnlineInfo onlineInfo)
    {
        return new PluginInfo(onlineInfo.Name, onlineInfo.Version, onlineInfo.Dependencies.Select(dep => dep.DownloadLocation).ToList());
    }
}
