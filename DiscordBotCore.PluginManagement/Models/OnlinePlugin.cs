using System.Text.Json.Serialization;

namespace DiscordBotCore.PluginManagement.Models;

public class OnlinePlugin
{
    public int PluginId { get; private set; }
    public string PluginName { get; private set; }
    public string PluginDescription { get; private set; }
    public string LatestVersion { get; private set; }
    public string PluginAuthor { get; private set; }
    public string PluginLink { get; private set; }
    public int OperatingSystem { get; private set; }

    [JsonConstructor]
    public OnlinePlugin(int pluginId, string pluginName, string pluginDescription, string latestVersion,
        string pluginAuthor, string pluginLink, int operatingSystem)
    {
        PluginId = pluginId;
        PluginName = pluginName;
        PluginDescription = pluginDescription;
        LatestVersion = latestVersion;
        PluginAuthor = pluginAuthor;
        PluginLink = pluginLink;
        OperatingSystem = operatingSystem;
    }
}