using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement;

public interface IPluginManager
{
    Task<List<OnlinePlugin>> GetPluginsList();
    Task<OnlinePlugin?> GetPluginDataByName(string pluginName);
    Task AppendPluginToDatabase(LocalPlugin pluginData);
    Task<List<LocalPlugin>> GetInstalledPlugins();
    Task<bool> IsPluginInstalled(string pluginName);
    Task<bool> MarkPluginToUninstall(string pluginName);
    Task UninstallMarkedPlugins();
    Task<string?> GetDependencyLocation(string dependencyName);
    Task<string?> GetDependencyLocation(string dependencyName, string pluginName);
    string GenerateDependencyRelativePath(string pluginName, string dependencyPath);
    Task InstallPlugin(OnlinePlugin plugin, IProgress<InstallationProgressIndicator> progress);
    Task<Tuple<Dictionary<string, string>, List<OnlineDependencyInfo>>> GatherInstallDataForPlugin(OnlinePlugin plugin);
    Task SetEnabledStatus(string pluginName, bool status);
}