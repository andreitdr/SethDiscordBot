namespace DiscordBotCore.PluginManagement.Helpers;

public interface IPluginRepositoryConfiguration
{
    public string BaseUrl { get; }
    
    public string PluginRepositoryLocation { get; }
    public string DependenciesRepositoryLocation { get; }
}