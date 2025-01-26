namespace DiscordBotCore.Interfaces.PluginManagement;

public interface IPluginRepositoryConfiguration
{
    public string BaseUrl { get; }
    
    public string PluginRepositoryLocation { get; }
    public string DependenciesRepositoryLocation { get; }
    
    
}