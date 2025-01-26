using DiscordBotCore.Interfaces.PluginManagement;

namespace DiscordBotCore.Online.Helpers;

public class PluginRepositoryConfiguration : IPluginRepositoryConfiguration
{
    public static PluginRepositoryConfiguration Default => new ("http://localhost:5097/api/v1/", "plugins-repository/", "dependencies-repository/");
    
    public string BaseUrl { get; }
    public string PluginRepositoryLocation { get; }
    public string DependenciesRepositoryLocation { get; }
    
    public PluginRepositoryConfiguration(string baseUrl, string pluginRepositoryLocation, string dependenciesRepositoryLocation)
    {
        BaseUrl = baseUrl;
        PluginRepositoryLocation = pluginRepositoryLocation;
        DependenciesRepositoryLocation = dependenciesRepositoryLocation;
    }
}