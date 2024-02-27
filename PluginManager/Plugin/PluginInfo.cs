using System.IO;
using PluginManager.Interfaces.Updater;
using PluginManager.Online.Helpers;

namespace PluginManager.Plugin;

public class PluginInfo
{
    public string PluginName { get; private set; }
    public PluginVersion PluginVersion { get; private set; }
    public string FilePath { get; private set; }

    public PluginInfo(string pluginName, PluginVersion pluginVersion)
    {
        PluginName    = pluginName;
        PluginVersion = pluginVersion;

        FilePath = $"{Config.AppSettings["PluginFolder"]}/{pluginName}.dll";
    }

    public static PluginInfo FromOnlineInfo(PluginOnlineInfo onlineInfo)
    {
        return new PluginInfo(onlineInfo.Name, onlineInfo.Version);
    }
}
