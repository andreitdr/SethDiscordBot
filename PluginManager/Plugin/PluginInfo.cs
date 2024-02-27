using System.IO;
using PluginManager.Interfaces.Updater;

namespace PluginManager.Plugin;

public class PluginInfo
{
    public string PluginName { get; private set; }
    public IVersion PluginVersion { get; private set; }
    public FileInfo FileData { get; private set; }

    public PluginInfo(string pluginName, IVersion pluginVersion)
    {
        PluginName    = pluginName;
        PluginVersion = pluginVersion;

        FileData = new FileInfo($"{Config.AppSettings["PluginFolder"]}/{pluginName}.dll");
    }

    public static PluginInfo FromOnlineInfo(PluginOnlineInfo onlineInfo)
    {
        return new PluginInfo(onlineInfo.Name, onlineInfo.Version);
    }
}
