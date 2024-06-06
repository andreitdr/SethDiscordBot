using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DiscordBotCore.Online.Helpers;
using DiscordBotCore.Others;

namespace DiscordBotCore.Plugin;

public class PluginOnlineInfo
{
    public string Name { get; private set; }
    public PluginVersion Version { get; private set; }
    public string DownLoadLink { get; private set; }
    public string Description { get; private set; }
    public List<OnlineDependencyInfo> Dependencies { get; private set; }
    public List<OnlineScriptDependencyInfo> ScriptDependencies { get; private set; }
    public OSType SupportedOS { get; private set; }
    public bool HasFileDependencies => Dependencies is not null && Dependencies.Count > 0;
    public bool HasScriptDependencies => ScriptDependencies is not null && ScriptDependencies.Count > 0;

    [JsonConstructor]
    public PluginOnlineInfo(string name, PluginVersion version, string description, string downLoadLink, OSType supportedOS, List<OnlineDependencyInfo> dependencies, List<OnlineScriptDependencyInfo> scriptDependencies)
    {
        Name = name;
        Version = version;
        Description = description;
        DownLoadLink = downLoadLink;
        SupportedOS = supportedOS;
        Dependencies = dependencies;
        ScriptDependencies = scriptDependencies;
    }

    public PluginOnlineInfo(string name, PluginVersion version, string description, string downLoadLink, OSType supportedOS)
    {
        Name            = name;
        Version         = version;
        Description     = description;
        DownLoadLink    = downLoadLink;
        SupportedOS     = supportedOS;
        Dependencies    = new List<OnlineDependencyInfo>();
        ScriptDependencies = new List<OnlineScriptDependencyInfo>();
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
