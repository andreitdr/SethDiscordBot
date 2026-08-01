using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities.Responses;

namespace DiscordBotCore.PluginManagement;

public interface IPluginManager
{
    Task<List<OnlinePlugin>> GetOnlinePluginsList();
    Task<IResponse<OnlinePlugin>> GetOnlinePluginDataByName(string pluginName);
    Task<IResponse<OnlinePlugin>> GetOnlinePluginDataById(int pluginId);
    Task<IResponse<bool>> AppendPluginToDatabase(LocalPlugin pluginData, List<LocalDependencyInfo> dependencies);
    Task<List<LocalPlugin>> GetInstalledPlugins();
    Task<IResponse<string>> GetDependencyLocation(string dependencyName, string pluginName);
    Task<IResponse<bool>> InstallPlugin(OnlinePlugin plugin, IProgress<float> progress);
    Task SetEnabledStatus(string pluginName, bool status);
    Task<IResponse<bool>> UninstallPluginByName(string pluginName);
    Task<LocalPlugin> CreateOfflineLocalPlugin(string pluginName, string location, string version, bool isEnabled);
}