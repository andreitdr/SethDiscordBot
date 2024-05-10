using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Online;
using PluginManager.Plugin;

namespace PluginManager.Updater.Plugins;

public class PluginUpdater
{
    private readonly PluginsManager _PluginsManager;
    
    public PluginUpdater(PluginsManager pluginManager)
    {
        _PluginsManager = pluginManager;
    }
    
    public async Task<PluginOnlineInfo> GetPluginInfo(string pluginName)
    {
        var result = await _PluginsManager.GetPluginDataByName(pluginName);
        return result;
    }
    
    public async Task<PluginInfo> GetLocalPluginInfo(string pluginName)
    {
        string           pluginsDatabase  = File.ReadAllText(Config.AppSettings["PluginDatabase"]);
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(pluginsDatabase);
        
        var result = installedPlugins.Find(p => p.PluginName == pluginName);
        
        return result;
    }

    public async Task UpdatePlugin(string pluginName, IProgress<float>? progressMeter = null)
    {
        PluginOnlineInfo pluginInfo = await GetPluginInfo(pluginName);
        await ServerCom.DownloadFileAsync(pluginInfo.DownLoadLink, $"{Config.AppSettings["PluginFolder"]}/{pluginName}.dll", progressMeter);
        
        foreach(OnlineDependencyInfo dependency in pluginInfo.Dependencies)
            await ServerCom.DownloadFileAsync(dependency.DownloadLocation, dependency.DownloadLocation, progressMeter);

        await _PluginsManager.RemovePluginFromDatabase(pluginName);
        await _PluginsManager.AppendPluginToDatabase(PluginInfo.FromOnlineInfo(pluginInfo));
    }

    public async Task<bool> HasUpdate(string pluginName)
    {
        var localPluginInfo = await GetLocalPluginInfo(pluginName);
        var pluginInfo = await GetPluginInfo(pluginName);

        return pluginInfo.Version.IsNewerThan(localPluginInfo.PluginVersion);

    }
    
}
