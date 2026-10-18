namespace DiscordBotCore.PluginManagement.Helpers;

public interface IPluginRepositoryConfiguration
{
    public string BaseUrl { get; }
    
    public string PluginRepositoryLocation { get; }
    public string DependenciesRepositoryLocation { get; }

    /// <summary>
    /// True when <see cref="BaseUrl"/> is an absolute http(s) URL.
    /// </summary>
    public bool IsConfigured { get; }
}
