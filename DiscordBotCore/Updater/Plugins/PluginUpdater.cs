using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DiscordBotCore.Online;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;
using DiscordBotCore.Plugin;

namespace DiscordBotCore.Updater.Plugins;

public class PluginUpdater
{
    private readonly PluginManager _PluginsManager;
    
    public PluginUpdater(PluginManager pluginManager)
    {
        _PluginsManager = pluginManager;
    }
    
    public async Task<PluginOnlineInfo> GetPluginInfo(string pluginName)
    {
        var result = await _PluginsManager.GetPluginDataByName(pluginName);
        if(result is null)
            throw new PluginNotFoundException(pluginName, _PluginsManager.BaseUrl, _PluginsManager.Branch);
        return result;
    }
    
    public async Task<PluginInfo> GetLocalPluginInfo(string pluginName)
    {
        string           pluginsDatabase  = File.ReadAllText(DiscordBotCore.Application.CurrentApplication.PluginDatabase);
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(pluginsDatabase);
        
        var result = installedPlugins.Find(p => p.PluginName == pluginName);
        if (result is null)
            throw new PluginNotFoundException(pluginName);
        return result;
    }

    public async Task UpdatePlugin(string pluginName, IProgress<float>? progressMeter = null)
    {
        PluginOnlineInfo pluginInfo = await GetPluginInfo(pluginName);
        await ServerCom.DownloadFileAsync(pluginInfo.DownLoadLink, $"{DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"]}/{pluginName}.dll", progressMeter);
        
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
