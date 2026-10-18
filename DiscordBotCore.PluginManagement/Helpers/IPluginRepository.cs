using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement.Helpers;

public interface IPluginRepository
{
    /// <summary>
    /// False when no repository URL is configured. The methods below then return nothing without making a request.
    /// </summary>
    public bool IsConfigured { get; }

    public Task<List<OnlinePlugin>> GetAllPlugins(int operatingSystem, bool includeNotApproved);

    public Task<OnlinePlugin?> GetPluginById(int pluginId);
    public Task<OnlinePlugin?> GetPluginByName(string pluginName, int operatingSystem, bool includeNotApproved);
    
    public Task<List<OnlineDependencyInfo>> GetDependenciesForPlugin(int pluginId);

}
