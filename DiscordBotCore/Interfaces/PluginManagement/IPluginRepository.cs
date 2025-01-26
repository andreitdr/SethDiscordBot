using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Plugin;

namespace DiscordBotCore.Interfaces.PluginManagement;

public interface IPluginRepository
{
    public Task<List<OnlinePlugin>> GetAllPlugins();

    public Task<OnlinePlugin?> GetPluginById(int pluginId);
    public Task<OnlinePlugin?> GetPluginByName(string pluginName);
    
    public Task<List<OnlineDependencyInfo>> GetDependenciesForPlugin(int pluginId);

}