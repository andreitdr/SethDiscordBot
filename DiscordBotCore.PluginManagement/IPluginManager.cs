using DiscordBotCore.PluginManagement.Models;

namespace DiscordBotCore.PluginManagement;

public interface IPluginManager
{
    Task<List<OnlinePlugin>> GetPluginsList();
    Task<OnlinePlugin?> GetPluginDataByName(string pluginName);
    Task<OnlinePlugin?> GetPluginDataById(int pluginId);
    Task AppendPluginToDatabase(LocalPlugin pluginData);
    Task<List<LocalPlugin>> GetInstalledPlugins();
    Task<bool> IsPluginInstalled(string pluginName);
    Task<string?> GetDependencyLocation(string dependencyName);
    Task<string?> GetDependencyLocation(string dependencyName, string pluginName);
    string GenerateDependencyRelativePath(string pluginName, string dependencyPath);
    Task InstallPlugin(OnlinePlugin plugin, IProgress<float> progress);
    Task SetEnabledStatus(string pluginName, bool status);
    Task<bool> UninstallPluginByName(string pluginName);
    Task<LocalPlugin?> GetLocalPluginByName(string pluginName);
}