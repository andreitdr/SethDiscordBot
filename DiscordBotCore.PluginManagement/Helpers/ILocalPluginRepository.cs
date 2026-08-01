using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement.Helpers;

public interface ILocalPluginRepository
{
    public Task<List<LocalPlugin>> GetAllPluginsAsync();
    public Task<LocalPlugin?> GetPluginByNameAsync(string name);
    public Task<List<LocalDependencyInfo>> GetDependenciesForPluginAsync(int pluginId);
    public Task<List<LocalDependencyInfo>> GetDependenciesForPluginAsync(string pluginName);
    
    public Task<Guid> AddPluginAsync(LocalPlugin plugin);
    public Task<bool> UpdatePluginAsync(LocalPlugin plugin);
    public Task<bool> DeletePluginAsync(Guid pluginId);
    
    public Task<Guid> AddDependencyAsync(LocalDependencyInfo dependency);
    public Task<bool> DeleteDependencyAsync(Guid dependencyId);
    
    public Task<bool> DeleteAllDependenciesForPluginAsync(Guid pluginId);

}