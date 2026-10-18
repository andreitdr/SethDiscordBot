using System.Text.Json.Serialization;

namespace DiscordBotCore.PluginManagement.Helpers;

public class PluginRepositoryConfiguration : IPluginRepositoryConfiguration
{
    /// <summary>
    /// No repository: the marketplace stays empty and nothing is requested until a BaseUrl is configured.
    /// </summary>
    public static PluginRepositoryConfiguration Default => new (string.Empty,
        "plugin/", 
        "dependency/");
    
    public string BaseUrl { get; }
    public string PluginRepositoryLocation { get; }
    public string DependenciesRepositoryLocation { get; }

    [JsonIgnore]
    public bool IsConfigured =>
        Uri.TryCreate(BaseUrl, UriKind.Absolute, out Uri? uri) &&
        (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    
    [JsonConstructor]
    public PluginRepositoryConfiguration(string baseUrl, string pluginRepositoryLocation, string dependenciesRepositoryLocation)
    {
        BaseUrl = baseUrl;
        PluginRepositoryLocation = pluginRepositoryLocation;
        DependenciesRepositoryLocation = dependenciesRepositoryLocation;
    }
}
