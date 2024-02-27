using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using PluginManager.Online.Helpers;
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

    public async Task<List<PluginOnlineInfo?>> GetPluginsList()
    {
        var                     jsonText = await ServerCom.GetAllTextFromUrl(PluginsLink);
        List<PluginOnlineInfo?> result   = await JsonManager.ConvertFromJson<List<PluginOnlineInfo?>>(jsonText);

        var currentOS = OperatingSystem.IsWindows() ? OSType.WINDOWS :
                        OperatingSystem.IsLinux()   ? OSType.LINUX : OSType.MACOSX;

        return result.FindAll(pl => (pl.SupportedOS & currentOS) != 0);
    }

    public async Task<PluginOnlineInfo?> GetPluginDataByName(string pluginName)
    {
        List<PluginOnlineInfo?> plugins = await GetPluginsList();
        var                     result  = plugins.Find(p => p.Name == pluginName);

        return result;
    }
    
    public async Task RemovePluginFromDatabase(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));
        
        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile( Config.AppSettings["PluginDatabase"],installedPlugins);
    }

    public async Task AppendPluginToDatabase(string pluginName, PluginVersion version)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));
        
        installedPlugins.Add(new PluginInfo(pluginName, version));
        await JsonManager.SaveToJsonFile( Config.AppSettings["PluginDatabase"],installedPlugins);
    }
    
    public async Task<List<PluginInfo>> GetInstalledPlugins()
    {
        return await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Config.AppSettings["PluginDatabase"]));
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
    

}
