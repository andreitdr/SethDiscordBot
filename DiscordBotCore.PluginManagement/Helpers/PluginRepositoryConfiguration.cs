using System.Text.Json.Serialization;

namespace DiscordBotCore.PluginManagement.Helpers;

public class PluginRepositoryConfiguration : IPluginRepositoryConfiguration
{
    public static PluginRepositoryConfiguration Default => new ("http://localhost:8080/api/v1/",
        "plugin/", 
        "dependency/");
    
    public string BaseUrl { get; }
    public string PluginRepositoryLocation { get; }
    public string DependenciesRepositoryLocation { get; }
    
    [JsonConstructor]
    public PluginRepositoryConfiguration(string baseUrl, string pluginRepositoryLocation, string dependenciesRepositoryLocation)
    {
        BaseUrl = baseUrl;
        PluginRepositoryLocation = pluginRepositoryLocation;
        DependenciesRepositoryLocation = dependenciesRepositoryLocation;
    }
}