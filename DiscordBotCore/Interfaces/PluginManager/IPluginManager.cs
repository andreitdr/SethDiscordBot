using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Plugin;

namespace DiscordBotCore.Interfaces.PluginManager
{
    public interface IPluginManager
    {
        public string BaseUrl { get; set; }
        public string Branch { get; set; }

        Task AppendPluginToDatabase(PluginInfo pluginData);
        Task CheckForUpdates();
        Task ExecutePluginInstallScripts(List<OnlineScriptDependencyInfo> listOfDependencies);
        string GenerateDependencyRelativePath(string pluginName, string dependencyPath);
        Task<string?> GetDependencyLocation(string dependencyName);
        Task<string?> GetDependencyLocation(string pluginName, string dependencyName);
        Task<List<PluginInfo>> GetInstalledPlugins();
        Task<PluginOnlineInfo?> GetPluginDataByName(string pluginName);
        Task<List<PluginOnlineInfo>?> GetPluginsList();
        Task InstallPlugin(PluginOnlineInfo pluginData, IProgress<float>? installProgress);
        Task<bool> IsPluginInstalled(string pluginName);
        Task<bool> MarkPluginToUninstall(string pluginName);
        Task RemovePluginFromDatabase(string pluginName);
        Task UninstallMarkedPlugins();

        Task SetEnabledStatus(string pluginName, bool status);
    }
}