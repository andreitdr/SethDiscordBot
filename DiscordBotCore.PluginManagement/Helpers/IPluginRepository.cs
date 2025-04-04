using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement.Helpers;

public interface IPluginRepository
{
    public Task<List<OnlinePlugin>> GetAllPlugins(int operatingSystem, bool includeNotApproved);

    public Task<OnlinePlugin?> GetPluginById(int pluginId);
    public Task<OnlinePlugin?> GetPluginByName(string pluginName, int operatingSystem, bool includeNotApproved);
    
    public Task<List<OnlineDependencyInfo>> GetDependenciesForPlugin(int pluginId);

}