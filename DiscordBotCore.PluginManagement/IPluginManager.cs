using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities.Responses;

namespace DiscordBotCore.PluginManagement;

public interface IPluginManager
{
    Task<List<OnlinePlugin>> GetPluginsList();
    Task<IResponse<OnlinePlugin>> GetPluginDataByName(string pluginName);
    Task<IResponse<OnlinePlugin>> GetPluginDataById(int pluginId);
    Task<IResponse<bool>> AppendPluginToDatabase(LocalPlugin pluginData);
    Task<List<LocalPlugin>> GetInstalledPlugins();
    Task<IResponse<string>> GetDependencyLocation(string dependencyName);
    Task<IResponse<string>> GetDependencyLocation(string dependencyName, string pluginName);
    string GenerateDependencyRelativePath(string pluginName, string dependencyPath);
    Task<IResponse<bool>> InstallPlugin(OnlinePlugin plugin, IProgress<float> progress);
    Task SetEnabledStatus(string pluginName, bool status);
    Task<IResponse<bool>> UninstallPluginByName(string pluginName);
    Task<IResponse<LocalPlugin>> GetLocalPluginByName(string pluginName);
}