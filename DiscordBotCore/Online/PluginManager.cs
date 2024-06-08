using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;
using DiscordBotCore.Updater.Plugins;

namespace DiscordBotCore.Online;

public class PluginManager
{
    private static readonly string _DefaultBranch  = "releases";
    private static readonly string _DefaultBaseUrl = "https://raw.githubusercontent.com/andreitdr/SethPlugins";

    private static readonly string _DefaultPluginsLink = "PluginsList.json";


    public string Branch { get; set; }
    public string BaseUrl { get; set; }


    private string PluginsLink => $"{BaseUrl}/{Branch}/{_DefaultPluginsLink}";

    public PluginManager(Uri baseUrl, string branch)
    {
        BaseUrl = baseUrl.ToString();
        Branch  = branch;
    }

    public PluginManager(string branch)
    {
        BaseUrl = _DefaultBaseUrl;
        Branch  = branch;
    }

    public PluginManager()
    {
        BaseUrl = _DefaultBaseUrl;
        Branch  = _DefaultBranch;
    }

    public async Task<List<PluginOnlineInfo>?> GetPluginsList()
    {
        var                    jsonText = await ServerCom.GetAllTextFromUrl(PluginsLink);
        List<PluginOnlineInfo> result   = await JsonManager.ConvertFromJson<List<PluginOnlineInfo>>(jsonText);

        var currentOS = OperatingSystem.IsWindows() ? OSType.WINDOWS :
                        OperatingSystem.IsLinux()   ? OSType.LINUX : 
                        OperatingSystem.IsMacOS()   ? OSType.MACOSX : OSType.NONE;

        return result.FindAll(pl => (pl.SupportedOS & currentOS) != 0);


    }

    public async Task<PluginOnlineInfo?> GetPluginDataByName(string pluginName)
    {
        List<PluginOnlineInfo>? plugins = await GetPluginsList();
        
        if(plugins == null)
            return null;

        // try to get the best matching plugin using the pluginName as a search query
        PluginOnlineInfo? result = plugins.Find(pl => pl.Name.ToLower().Contains(pluginName.ToLower()));
        if(result == null) return null;

        return result;
    }
    
    public async Task RemovePluginFromDatabase(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));
        
        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile(Application.CurrentApplication.PluginDatabase,installedPlugins);
    }

    public async Task ExecutePluginInstallScripts(List<OnlineScriptDependencyInfo> listOfDependencies)
    {
        string consoleType = OperatingSystem.IsWindows() ? "cmd.exe" : "bash";
        foreach(var script in listOfDependencies)
            await ServerCom.RunConsoleCommand(consoleType, "/c " + script.ScriptContent);
    }

    public async Task AppendPluginToDatabase(PluginInfo pluginData)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));
        foreach (var dependency in pluginData.ListOfDependancies)
        {
            pluginData.ListOfDependancies[dependency.Key] = GenerateDependencyLocation(pluginData.PluginName, dependency.Value);
        }

        installedPlugins.Add(pluginData);
        await JsonManager.SaveToJsonFile(Application.CurrentApplication.PluginDatabase, installedPlugins);
    }
    
    public async Task<List<PluginInfo>> GetInstalledPlugins()
    {
        return await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));
    }

    public async Task<bool> IsPluginInstalled(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));

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
                Application.CurrentApplication.Logger.Log("Updating plugin: " + plugin.PluginName, this, LogType.INFO);
                await pluginUpdater.UpdatePlugin(plugin.PluginName);
            }
        }
    }

    public async Task<bool> MarkPluginToUninstall(string pluginName)
    {
        IEnumerable<PluginInfo> installedPlugins = await GetInstalledPlugins();
        IEnumerable<PluginInfo> info = installedPlugins.Where(info => info.PluginName == pluginName).AsEnumerable();

        if(!info.Any())
            return false;

        foreach (var item in info)
        {
            await RemovePluginFromDatabase(item.PluginName);
            item.IsMarkedToUninstall = true;
            await AppendPluginToDatabase(item);
        }


        return true;

    }

    public async Task UninstallMarkedPlugins()
    {
        IEnumerable<PluginInfo> installedPlugins = (await GetInstalledPlugins()).AsEnumerable();
        IEnumerable<PluginInfo> pluginsToRemove = installedPlugins.Where(plugin => plugin.IsMarkedToUninstall).AsEnumerable();

        foreach (var plugin in pluginsToRemove)
        {
            await UninstallPlugin(plugin);
        }
    }

    private async Task UninstallPlugin(PluginInfo pluginInfo)
    {
        File.Delete(pluginInfo.FilePath);

        foreach (var dependency in pluginInfo.ListOfDependancies)
            File.Delete(dependency.Value);

        await RemovePluginFromDatabase(pluginInfo.PluginName);
    }

    public async Task<string> GetDependencyLocation(string dependencyName)
    {
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();

        foreach (var plugin in installedPlugins)
        {
            if (plugin.ListOfDependancies.ContainsKey(dependencyName))
                return plugin.ListOfDependancies[dependencyName];
        }

        throw new Exception("Dependency not found");
    }

    public string GenerateDependencyLocation(string pluginName, string dependencyName)
    {
        return Path.Combine(Environment.CurrentDirectory, $"Libraries/{pluginName}/{dependencyName}");
    }

    public async Task InstallPlugin(PluginOnlineInfo pluginData, IProgress<float>? installProgress)
    {
        installProgress?.Report(0f);

        int totalSteps = pluginData.HasFileDependencies ? pluginData.Dependencies.Count + 1: 1;
        totalSteps += pluginData.HasScriptDependencies ? pluginData.ScriptDependencies.Count : 0;

        float stepProgress = 1f / totalSteps; 

        float currentProgress = 0f;

        IProgress<float> progress = new Progress<float>((p) => {
            installProgress?.Report(currentProgress + stepProgress * p);
        });

        await ServerCom.DownloadFileAsync(pluginData.DownLoadLink, $"{Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"]}/{pluginData.Name}.dll", progress);

        if (pluginData.HasFileDependencies)
            foreach (var dependency in pluginData.Dependencies)
            {
                string dependencyLocation = GenerateDependencyLocation(pluginData.Name, dependency.DownloadLocation);
                await ServerCom.DownloadFileAsync(dependency.DownloadLink, dependencyLocation, progress);
                
                currentProgress += stepProgress;
            }

        if (pluginData.HasScriptDependencies)
            foreach (var scriptDependency in pluginData.ScriptDependencies)
            {

                string console = OperatingSystem.IsWindows() ? "start cmd.exe" : "bash";
                string arguments = OperatingSystem.IsWindows() ? $"/c {scriptDependency.ScriptContent}" : scriptDependency.ScriptContent;

                await ServerCom.RunConsoleCommand(console, arguments);
                currentProgress += stepProgress;
            }

        PluginInfo pluginInfo = PluginInfo.FromOnlineInfo(pluginData);

        await AppendPluginToDatabase(pluginInfo);
    }



}
