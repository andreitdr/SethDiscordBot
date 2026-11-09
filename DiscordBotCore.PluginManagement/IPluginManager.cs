using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities.Responses;

namespace DiscordBotCore.PluginManagement;

public interface IPluginManager
{
    /// <summary>
    /// False when no plugin repository is configured: online lookups return nothing and installs fail.
    /// </summary>
    bool IsRepositoryConfigured { get; }

    /// <summary>
    /// Whether this bot and the configured repository can work together. Nothing else here contacts a repository unless
    /// they can. The answer is kept until <paramref name="recheck"/> is set or the BaseUrl changes.
    /// </summary>
    Task<RepositoryCompatibility> CheckRepositoryCompatibility(bool recheck = false);

    Task<List<OnlinePlugin>> GetOnlinePluginsList();
    Task<IResponse<OnlinePlugin>> GetOnlinePluginDataByName(string pluginName);
    Task<IResponse<OnlinePlugin>> GetOnlinePluginDataById(int pluginId);
    Task<IResponse<bool>> AppendPluginToDatabase(LocalPlugin pluginData, List<LocalDependencyInfo> dependencies);
    Task<List<LocalPlugin>> GetInstalledPlugins();
    Task<IResponse<string>> GetDependencyLocation(string dependencyName, string pluginName);
    Task<IResponse<bool>> InstallPlugin(OnlinePlugin plugin, IProgress<float> progress);
    /// <summary>
    /// Turns a plugin on or off. Turning on a plugin that has a removal notice means its owner chose to keep it despite
    /// that notice, so it isn't turned off again for it.
    /// </summary>
    Task SetEnabledStatus(string pluginName, bool status);

    /// <summary>
    /// Asks the repository whether the installed marketplace plugins were removed from it, saves the answer, logs a
    /// warning for each removed one and turns off the ones removed as unsafe that their owner hasn't chosen to keep.
    /// When the repository can't answer, the last saved answer is used. Returns the removed plugins.
    /// </summary>
    Task<List<LocalPlugin>> CheckRemovedPluginsAsync();
    Task<IResponse<bool>> UninstallPluginByName(string pluginName);
    Task<LocalPlugin> CreateOfflineLocalPlugin(string pluginName, string location, string version, bool isEnabled);
}