using DiscordBotCore.Logging;
using DiscordBotCore.Networking;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities;
using DiscordBotCore.Configuration;
using OperatingSystem = DiscordBotCore.Utilities.OperatingSystem;

namespace DiscordBotCore.PluginManagement;

public sealed class PluginManager : IPluginManager
{
    private static readonly string _LibrariesBaseFolder = "Libraries";
    private readonly IPluginRepository _PluginRepository;
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;
    
    public PluginManager(IPluginRepository pluginRepository, ILogger logger, IConfiguration configuration)
    {
        _PluginRepository = pluginRepository;
        _Logger = logger;
        _Configuration = configuration;
    }

    public async Task<List<OnlinePlugin>> GetPluginsList()
    {
        int os = OperatingSystem.GetOperatingSystemInt();
        var onlinePlugins = await _PluginRepository.GetAllPlugins(os, false);
        
        if (!onlinePlugins.Any())
        {
            _Logger.Log($"No plugins found for operatingSystem: {OperatingSystem.GetOperatingSystemString((OperatingSystem.OperatingSystemEnum)os)}", LogType.Warning);
            return [];
        }

        return onlinePlugins;
    }

    public async Task<OnlinePlugin?> GetPluginDataByName(string pluginName)
    {
        int os = OperatingSystem.GetOperatingSystemInt();
        var plugin = await _PluginRepository.GetPluginByName(pluginName, os, false);

        if (plugin == null)
        {
            _Logger.Log($"Plugin {pluginName} not found in the repository for operating system {OperatingSystem.GetOperatingSystemString((OperatingSystem.OperatingSystemEnum)os)}.", LogType.Warning);
            return null;
        }

        return plugin;
    }

    private async Task RemovePluginFromDatabase(string pluginName)
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");

        if (pluginDatabaseFile is null)
        {
            throw new Exception("Plugin database file not found");
        }
        
        List<LocalPlugin> installedPlugins = await JsonManager.ConvertFromJson<List<LocalPlugin>>(await File.ReadAllTextAsync(pluginDatabaseFile));

        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile(pluginDatabaseFile, installedPlugins);
    }

    public async Task AppendPluginToDatabase(LocalPlugin pluginData)
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");
        if (pluginDatabaseFile is null)
        {
            throw new Exception("Plugin database file not found");
        }
        
        List<LocalPlugin> installedPlugins = await JsonManager.ConvertFromJson<List<LocalPlugin>>(await File.ReadAllTextAsync(pluginDatabaseFile));
        foreach (var dependency in pluginData.ListOfExecutableDependencies)
        {
            pluginData.ListOfExecutableDependencies[dependency.Key] = dependency.Value;
        }

        installedPlugins.Add(pluginData);
        await JsonManager.SaveToJsonFile(pluginDatabaseFile, installedPlugins);
    }

    public async Task<List<LocalPlugin>> GetInstalledPlugins()
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");
        if (pluginDatabaseFile is null)
        {
            throw new Exception("Plugin database file not found");
        }
        
        return await JsonManager.ConvertFromJson<List<LocalPlugin>>(await File.ReadAllTextAsync(pluginDatabaseFile));
    }

    public async Task<bool> IsPluginInstalled(string pluginName)
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");
        if (pluginDatabaseFile is null)
        {
            throw new Exception("Plugin database file not found");
        }
        
        List<LocalPlugin> installedPlugins = await JsonManager.ConvertFromJson<List<LocalPlugin>>(await File.ReadAllTextAsync(pluginDatabaseFile));
        return installedPlugins.Any(plugin => plugin.PluginName == pluginName);
    }

    public async Task<bool> MarkPluginToUninstall(string pluginName)
    {
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();
        List<LocalPlugin> info = installedPlugins.Where(info => info.PluginName == pluginName).ToList();

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
        IEnumerable<LocalPlugin> installedPlugins = (await GetInstalledPlugins()).AsEnumerable();
        IEnumerable<LocalPlugin> pluginsToRemove = installedPlugins.Where(plugin => plugin.IsMarkedToUninstall).AsEnumerable();

        foreach (var plugin in pluginsToRemove)
        {
            await UninstallPlugin(plugin);
        }
    }

    private async Task UninstallPlugin(LocalPlugin LocalPlugin)
    {
        File.Delete(LocalPlugin.FilePath);

        foreach (var dependency in LocalPlugin.ListOfExecutableDependencies)
            File.Delete(dependency.Value);

        await RemovePluginFromDatabase(LocalPlugin.PluginName);

        if (Directory.Exists($"{_LibrariesBaseFolder}/{LocalPlugin.PluginName}"))
            Directory.Delete($"{_LibrariesBaseFolder}/{LocalPlugin.PluginName}", true);
    }

    public async Task<string?> GetDependencyLocation(string dependencyName)
    {
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();

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
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();

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

    public async Task InstallPlugin(OnlinePlugin plugin, IProgress<InstallationProgressIndicator> progress)
    {
        List<OnlineDependencyInfo> dependencies = await _PluginRepository.GetDependenciesForPlugin(plugin.PluginId);
        string? pluginsFolder = _Configuration.Get<string>("PluginFolder");
        if (pluginsFolder is null)
        {
            throw new Exception("Plugin folder not found");
        }
        
        string downloadLocation = $"{pluginsFolder}/{plugin.PluginName}.dll";
        
        InstallationProgressIndicator installationProgressIndicator = new InstallationProgressIndicator();
        
        IProgress<float> downloadProgress = new Progress<float>(fileProgress =>
        {
            installationProgressIndicator.SetProgress(plugin.PluginName, fileProgress);
            progress.Report(installationProgressIndicator);
        });
        
        FileDownloader fileDownloader = new FileDownloader(plugin.PluginLink, downloadLocation);
        await fileDownloader.DownloadFile(downloadProgress.Report);

        ParallelDownloadExecutor executor = new ParallelDownloadExecutor();

        foreach (var dependency in dependencies)
        {
            string dependencyLocation = GenerateDependencyRelativePath(plugin.PluginName, dependency.DownloadLocation);
            Action<float> dependencyProgress = new Action<float>(fileProgress =>
            {
                installationProgressIndicator.SetProgress(dependency.DependencyName, fileProgress);
                progress.Report(installationProgressIndicator);
            });
            
            executor.AddTask(dependency.DownloadLink, dependencyLocation, dependencyProgress);
        }
        
        await executor.ExecuteAllTasks();
    }
    
    public async Task<Tuple<Dictionary<string, string>, List<OnlineDependencyInfo>>> GatherInstallDataForPlugin(OnlinePlugin plugin)
    {
        List<OnlineDependencyInfo> dependencies = await _PluginRepository.GetDependenciesForPlugin(plugin.PluginId);
        string? pluginsFolder = _Configuration.Get<string>("PluginFolder");
        if (pluginsFolder is null)
        {
            throw new Exception("Plugin folder not found");
        }
        string downloadLocation = $"{pluginsFolder}/{plugin.PluginName}.dll";
        var downloads = new Dictionary<string, string> { { downloadLocation, plugin.PluginLink } };
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
