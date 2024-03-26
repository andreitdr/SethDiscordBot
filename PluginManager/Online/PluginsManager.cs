using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using PluginManager.Others;
using PluginManager.Plugin;
using PluginManager.Updater.Plugins;

namespace PluginManager.Online;

public class PluginsManager
{
    private static readonly string _DefaultBranch  = "releases";
    private static readonly string _DefaultBaseUrl = "https://raw.githubusercontent.com/andreitdr/SethPlugins";

    private static readonly string _DefaultPluginsLink = "PluginsList.json";


    public string Branch { get; init; }
    public string BaseUrl { get; init; }


    private string PluginsLink => $"{BaseUrl}/{Branch}/{_DefaultPluginsLink}";

    public PluginsManager(Uri baseUrl, string branch)
    {
        BaseUrl = baseUrl.ToString();
        Branch  = branch;
    }

    public PluginsManager(string branch)
    {
        BaseUrl = _DefaultBaseUrl;
        Branch  = branch;
    }

    public PluginsManager()
    {
        BaseUrl = _DefaultBaseUrl;
        Branch  = _DefaultBranch;
    }

    public async Task<List<PluginOnlineInfo>?> GetPluginsList()
    {
        var                    jsonText = await ServerCom.GetAllTextFromUrl(PluginsLink);
        List<PluginOnlineInfo> result   = await JsonManager.ConvertFromJson<List<PluginOnlineInfo>>(jsonText);

        var currentOS = OperatingSystem.IsWindows() ? OSType.WINDOWS :
                        OperatingSystem.IsLinux()   ? OSType.LINUX : OSType.MACOSX;

        return result.FindAll(pl => (pl.SupportedOS & currentOS) != 0);
    }

    public async Task<PluginOnlineInfo?> GetPluginDataByName(string pluginName)
    {
        List<PluginOnlineInfo>? plugins = await GetPluginsList();
        var                     result  = plugins?.Find(p => p.Name == pluginName);

        return result;
    }
    
    public async Task RemovePluginFromDatabase(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));
        
        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile( Config.AppSettings["PluginDatabase"],installedPlugins);
    }

    public async Task AppendPluginToDatabase(PluginInfo pluginData)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));
        
        installedPlugins.Add(pluginData);
        await JsonManager.SaveToJsonFile( Config.AppSettings["PluginDatabase"],installedPlugins);
    }
    
    public async Task<List<PluginInfo>> GetInstalledPlugins()
    {
        return await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));
    }

    public async Task<bool> IsPluginInstalled(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));

        return installedPlugins.Any(plugin => plugin.PluginName == pluginName);
    }

    public async Task CheckForUpdates()
    {
        var pluginUpdater = new PluginUpdater(this);
        
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();
        
        foreach (var plugin in installedPlugins)
        {
            if (await pluginUpdater.HasUpdate(plugin.PluginName))
            {
                Console.WriteLine($"Updating {plugin.PluginName}...");
                await pluginUpdater.UpdatePlugin(plugin.PluginName);
            }
        }
    }

    public async Task<bool> MarkPluginToUninstall(string pluginName)
    {
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();
        PluginInfo? info = installedPlugins.Find(info => info.PluginName == pluginName);

        if(info == null)
            return false;

        await RemovePluginFromDatabase(pluginName);
        info.IsMarkedToUninstall = true;
        await AppendPluginToDatabase(info);

        return true;

    }

    public async Task UninstallMarkedPlugins()
    {
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();
        foreach(PluginInfo plugin in installedPlugins)
        {
            if(!plugin.IsMarkedToUninstall) continue;

            await UninstallPlugin(plugin);
        }
    }

    private async Task UninstallPlugin(PluginInfo pluginInfo)
    {
        File.Delete(pluginInfo.FilePath);

        foreach(string dependency in pluginInfo.ListOfDependancies)
            File.Delete(dependency);

        await RemovePluginFromDatabase(pluginInfo.PluginName);
    }

    public async Task InstallPlugin(PluginOnlineInfo pluginData, IProgress<float>? installProgress)
    {
        installProgress?.Report(0f);

        int totalSteps = pluginData.HasDependencies ? pluginData.Dependencies.Count + 1 : 1;

        float stepProgress = 1f / totalSteps; 

        float currentProgress = 0f;

        IProgress<float> progress = new Progress<float>((p) => {
            installProgress?.Report(currentProgress + stepProgress * p);
        });

        await ServerCom.DownloadFileAsync(pluginData.DownLoadLink, $"{Config.AppSettings["PluginFolder"]}/{pluginData.Name}.dll", progress);

        foreach (var dependency in pluginData.Dependencies)
        {
            await ServerCom.DownloadFileAsync(dependency.DownloadLink, dependency.DownloadLocation, progress);
            currentProgress += stepProgress;
        }
    }



}
