using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginManager.Online.Helpers;
using PluginManager.Others;
using PluginManager.Plugin;

namespace PluginManager.Online;

public class PluginsManager
{
    private static readonly string _DefaultBranch = "releases";
    private static readonly string _DefaultBaseUrl = "https://raw.githubusercontent.com/andreitdr/SethPlugins";
    
    private static readonly string _DefaultPluginsLink = "PluginsList.json";
    
    
    public string Branch { get; init; }
    public string BaseUrl { get; init; }
    
    
    private string PluginsLink => $"{BaseUrl}/{Branch}/{_DefaultPluginsLink}";
    
    public PluginsManager(Uri baseUrl, string branch)
    {
        this.BaseUrl = baseUrl.ToString();
        this.Branch = branch;
    }
    
    public PluginsManager(string branch)
    {
        this.BaseUrl = _DefaultBaseUrl;
        this.Branch = branch;
    }
    
    public PluginsManager()
    {
        this.BaseUrl = _DefaultBaseUrl;
        this.Branch = _DefaultBranch;
    }

    public async Task<List<PluginOnlineInfo?>> GetPluginsList()
    {
        string jsonText = await ServerCom.GetAllTextFromUrl(PluginsLink);
        List<PluginOnlineInfo?> result = await JsonManager.ConvertFromJson<List<PluginOnlineInfo?>>(jsonText);
        
        OSType currentOS = OperatingSystem.IsWindows() ? OSType.WINDOWS : OperatingSystem.IsLinux() ? OSType.LINUX : OSType.MACOSX;

        return result.FindAll(pl => (pl.SupportedOS & currentOS) != 0);
    }

    public async Task<PluginOnlineInfo?> GetPluginDataByName(string pluginName)
    {
        List<PluginOnlineInfo?> plugins = await GetPluginsList(); 
        PluginOnlineInfo? result = plugins.Find(p => p.Name == pluginName);

        return result;
    }

   
}
