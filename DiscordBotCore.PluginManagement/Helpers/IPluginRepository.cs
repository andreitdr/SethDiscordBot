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

    /// <summary>
    /// The NuGet packages a plugin needs. Null when the repository couldn't answer, so that a plugin isn't installed
    /// without its packages. Empty when it needs none.
    /// </summary>
    public Task<List<OnlineNuGetDependency>?> GetNuGetDependenciesForPlugin(int pluginId);

    /// <summary>
    /// Whether this bot and the repository can work together. Nothing else here makes a request unless they can.
    /// The answer is kept until <paramref name="recheck"/> is set or the BaseUrl changes; Unreachable is never kept.
    /// </summary>
    public Task<RepositoryCompatibility> CheckCompatibilityAsync(bool recheck = false);

}
