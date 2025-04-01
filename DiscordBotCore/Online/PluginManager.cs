using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.PluginManagement;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;

namespace DiscordBotCore.Online;

public interface IPluginManager
{
    Task<List<OnlinePlugin>> GetPluginsList();
    Task<OnlinePlugin?> GetPluginDataByName(string pluginName);
    Task AppendPluginToDatabase(PluginInfo pluginData);
    Task<List<PluginInfo>> GetInstalledPlugins();
    Task<bool> IsPluginInstalled(string pluginName);
    Task<bool> MarkPluginToUninstall(string pluginName);
    Task UninstallMarkedPlugins();
    Task<string?> GetDependencyLocation(string dependencyName);
    Task<string?> GetDependencyLocation(string dependencyName, string pluginName);
    string GenerateDependencyRelativePath(string pluginName, string dependencyPath);
    Task InstallPluginNoProgress(OnlinePlugin plugin);
    Task<Tuple<Dictionary<string, string>, List<OnlineDependencyInfo>>> GatherInstallDataForPlugin(OnlinePlugin plugin);
    Task SetEnabledStatus(string pluginName, bool status);
}

public sealed class PluginManager : IPluginManager
{
    private static readonly string _LibrariesBaseFolder = "Libraries";
    private readonly IPluginRepository _PluginRepository;
    internal InstallingPluginInformation? InstallingPluginInformation { get; private set; }
    
    internal PluginManager(IPluginRepository pluginRepository)
    {
        _PluginRepository = pluginRepository;
    }

    public async Task<List<OnlinePlugin>> GetPluginsList()
    {
        var onlinePlugins = await _PluginRepository.GetAllPlugins();
        
        if (!onlinePlugins.Any())
        {
            Application.Log("Could not get any plugins from the repository", LogType.Warning);
            return [];
        }
        
        int os = OS.GetOperatingSystemInt();

        var response = onlinePlugins.Where(plugin => plugin.OperatingSystem == os).ToList();

        return response;
    }

    public async Task<OnlinePlugin?> GetPluginDataByName(string pluginName)
    {
        var plugin = await _PluginRepository.GetPluginByName(pluginName);

        if (plugin == null)
        {
            Application.Log("Failed to get plugin from the repository", LogType.Warning);
            return null;
        }

        return plugin;
    }

    private async Task RemovePluginFromDatabase(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));

        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile(Application.CurrentApplication.PluginDatabase, installedPlugins);
    }

    public async Task AppendPluginToDatabase(PluginInfo pluginData)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));
        foreach (var dependency in pluginData.ListOfExecutableDependencies)
        {
            pluginData.ListOfExecutableDependencies[dependency.Key] = dependency.Value;
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

    public async Task<bool> MarkPluginToUninstall(string pluginName)
    {
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();
        List<PluginInfo> info = installedPlugins.Where(info => info.PluginName == pluginName).ToList();

        if (!info.Any())
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

        foreach (var dependency in pluginInfo.ListOfExecutableDependencies)
            File.Delete(dependency.Value);

        await RemovePluginFromDatabase(pluginInfo.PluginName);

        if (Directory.Exists($"{_LibrariesBaseFolder}/{pluginInfo.PluginName}"))
            Directory.Delete($"{_LibrariesBaseFolder}/{pluginInfo.PluginName}", true);
    }

    public async Task<string?> GetDependencyLocation(string dependencyName)
    {
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();

        foreach (var plugin in installedPlugins)
        {
            if (plugin.ListOfExecutableDependencies.TryGetValue(dependencyName, out var dependencyPath))
            {
                string relativePath = GenerateDependencyRelativePath(plugin.PluginName, dependencyPath);
                return relativePath;
            }
        }

        return null;
    }
    
    public async Task<string?> GetDependencyLocation(string dependencyName, string pluginName)
    {
        List<PluginInfo> installedPlugins = await GetInstalledPlugins();

        foreach (var plugin in installedPlugins)
        {
            if (plugin.PluginName == pluginName && plugin.ListOfExecutableDependencies.ContainsKey(dependencyName))
            {
                string dependencyPath     = plugin.ListOfExecutableDependencies[dependencyName];
                string relativePath = GenerateDependencyRelativePath(pluginName, dependencyPath); 
                return relativePath;
            }
        }

        return null;
    }

    public string GenerateDependencyRelativePath(string pluginName, string dependencyPath)
    {
        string relative = $"./{_LibrariesBaseFolder}/{pluginName}/{dependencyPath}";
        return relative;
    }

    public async Task InstallPluginNoProgress(OnlinePlugin plugin)
    {
        InstallingPluginInformation = new InstallingPluginInformation() { PluginName = plugin.PluginName };
        List<OnlineDependencyInfo> dependencies = await _PluginRepository.GetDependenciesForPlugin(plugin.PluginId);
        
        int totalSteps = dependencies.Count + 1;
        float stepFraction = 100f / totalSteps;
        float currentProgress = 0f;
        
        InstallingPluginInformation.IsInstalling = true;
        var progress = currentProgress;
        IProgress<float> downloadProgress = new Progress<float>(fileProgress =>
        {
            InstallingPluginInformation.InstallationProgress = progress + (fileProgress / 100f) * stepFraction;
        });
        
        await ServerCom.DownloadFileAsync(plugin.PluginLink,
            $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{plugin.PluginName}.dll",
            downloadProgress);
        
        currentProgress += stepFraction;

        if (dependencies.Count > 0)
        {
            foreach (var dependency in dependencies)
            {
                string dependencyLocation = GenerateDependencyRelativePath(plugin.PluginName, dependency.DownloadLocation);
                await ServerCom.DownloadFileAsync(dependency.DownloadLink, dependencyLocation, downloadProgress);
                currentProgress += stepFraction;
            }
        }
        
        PluginInfo pluginInfo = PluginInfo.FromOnlineInfo(plugin, dependencies);
        await AppendPluginToDatabase(pluginInfo);
        InstallingPluginInformation.IsInstalling = false;
    }
    
    public async Task<Tuple<Dictionary<string, string>, List<OnlineDependencyInfo>>> GatherInstallDataForPlugin(OnlinePlugin plugin)
    {
        List<OnlineDependencyInfo> dependencies = await _PluginRepository.GetDependenciesForPlugin(plugin.PluginId);
        var downloads = new Dictionary<string, string> { { $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{plugin.PluginName}.dll", plugin.PluginLink } };
        foreach(var dependency in dependencies)
        {
            string dependencyLocation = GenerateDependencyRelativePath(plugin.PluginName, dependency.DownloadLocation);
            downloads.Add(dependencyLocation, dependency.DownloadLink);
        }

        return (downloads, dependencies).ToTuple();
    }

    public async Task SetEnabledStatus(string pluginName, bool status)
    {
        var plugins = await GetInstalledPlugins();
        var plugin = plugins.Find(p => p.PluginName == pluginName);

        if (plugin == null)
            return;

        plugin.IsEnabled = status;

        await RemovePluginFromDatabase(pluginName);
        await AppendPluginToDatabase(plugin);

    }
}
