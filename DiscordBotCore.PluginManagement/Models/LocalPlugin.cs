using System.Text.Json.Serialization;

namespace DiscordBotCore.PluginManagement.Models;

public class LocalPlugin
{
    public string PluginName { get; private set; }
    public string PluginVersion { get; private set; }
    public string FilePath { get; private set; }
    public Dictionary<string, string> ListOfExecutableDependencies {get; private set;}
    public bool IsOfflineAdded { get; internal set; }
    public bool IsEnabled { get; internal set; }

    [JsonConstructor]
    public LocalPlugin(string pluginName, string pluginVersion, string filePath, Dictionary<string, string> listOfExecutableDependencies, bool isOfflineAdded, bool isEnabled)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        FilePath = filePath;
        IsOfflineAdded = isOfflineAdded;
        IsEnabled = isEnabled;
    }

    private LocalPlugin(string pluginName, string pluginVersion, string filePath,
        Dictionary<string, string> listOfExecutableDependencies)
    {
        PluginName = pluginName;
        PluginVersion = pluginVersion;
        ListOfExecutableDependencies = listOfExecutableDependencies;
        FilePath = filePath;
        IsOfflineAdded = false;
        IsEnabled = true;
    }

    public static LocalPlugin FromOnlineInfo(OnlinePlugin plugin, List<OnlineDependencyInfo> dependencies, string downloadLocation)
    {
        LocalPlugin localPlugin = new LocalPlugin(
            plugin.Name, plugin.Version, downloadLocation,
            dependencies.Where(dependency => dependency.IsExecutable)
                .ToDictionary(dependency => dependency.DependencyName, dependency => dependency.DownloadLocation)
        );
        
        return localPlugin;
    }
}
