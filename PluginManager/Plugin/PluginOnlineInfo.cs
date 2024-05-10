using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using PluginManager.Online.Helpers;
using PluginManager.Others;

namespace PluginManager.Plugin;

public class PluginOnlineInfo
{
    public string Name { get; private set; }
    public PluginVersion Version { get; private set; }
    public string DownLoadLink { get; private set; }
    public string Description { get; private set; }
    public List<OnlineDependencyInfo> Dependencies { get; private set; }
    public OSType SupportedOS { get; private set; }
    public bool HasDependencies { get; init; }

    [JsonConstructor]
    public PluginOnlineInfo(string name, PluginVersion version, string description, string downLoadLink, OSType supportedOS, List<OnlineDependencyInfo> dependencies)
    {
        Name            = name;
        Version         = version;
        Description     = description;
        DownLoadLink    = downLoadLink;
        SupportedOS     = supportedOS;
        Dependencies    = dependencies;
        HasDependencies = dependencies.Count > 0;
    }

    public PluginOnlineInfo(string name, PluginVersion version, string description, string downLoadLink, OSType supportedOS)
    {
        Name            = name;
        Version         = version;
        Description     = description;
        DownLoadLink    = downLoadLink;
        SupportedOS     = supportedOS;
        Dependencies    = new List<OnlineDependencyInfo>();
        HasDependencies = false;
    }

    public static async Task<PluginOnlineInfo> FromRawData(string jsonText)
    {
        return await JsonManager.ConvertFromJson<PluginOnlineInfo>(jsonText);
    }

    public override string ToString()
    {
        return $"{Name} - {Version} ({Description})";
    }
}
