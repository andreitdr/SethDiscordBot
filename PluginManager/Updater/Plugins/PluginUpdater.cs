using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Online;
using PluginManager.Plugin;

namespace PluginManager.Updater.Plugins;

public class PluginUpdater
{
    private readonly PluginsManager _PluginManager;
    
    public PluginUpdater(PluginsManager pluginManager)
    {
        _PluginManager = pluginManager;
    }
    
    public async Task<PluginOnlineInfo> GetPluginInfo(string pluginName)
    {
        var result = await _PluginManager.GetPluginDataByName(pluginName);
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

        await _PluginManager.RemovePluginFromDatabase(pluginName);
        await _PluginManager.AppendPluginToDatabase(pluginName, pluginInfo.Version);
    }

    public async Task<bool> HasUpdate(string pluginName)
    {
        var localPluginInfo = await GetLocalPluginInfo(pluginName);
        var pluginInfo = await GetPluginInfo(pluginName);

        return pluginInfo.Version.IsNewerThan(localPluginInfo.PluginVersion);

    }
    
}
