using System.Diagnostics;
using DiscordBotCore.Logging;
using DiscordBotCore.Networking;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities;
using DiscordBotCore.Configuration;
using DiscordBotCore.Utilities.Responses;
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

    public async Task<IResponse<OnlinePlugin>> GetPluginDataByName(string pluginName)
    {
        int os = OperatingSystem.GetOperatingSystemInt();
        var plugin = await _PluginRepository.GetPluginByName(pluginName, os, false);

        if (plugin is null)
        {
            return Response<OnlinePlugin>.Failure($"Plugin {pluginName} not found in the repository for operating system {OperatingSystem.GetOperatingSystemString((OperatingSystem.OperatingSystemEnum)os)}.");
        }

        return Response<OnlinePlugin>.Success(plugin);
    }

    public async Task<IResponse<OnlinePlugin>> GetPluginDataById(int pluginId)
    {
        var plugin = await _PluginRepository.GetPluginById(pluginId);
        if (plugin is null)
        {
            return Response<OnlinePlugin>.Failure($"Plugin {pluginId} not found in the repository.");
        }
        
        return Response<OnlinePlugin>.Success(plugin);
    }

    private async Task<IResponse<bool>> RemovePluginFromDatabase(string pluginName)
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");

        if (pluginDatabaseFile is null)
        {
            return Response.Failure("PluginDatabase file path is not present in the config file");
        }
        
        List<LocalPlugin> installedPlugins = await JsonManager.ConvertFromJson<List<LocalPlugin>>(await File.ReadAllTextAsync(pluginDatabaseFile));

        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile(pluginDatabaseFile, installedPlugins);
        
        return Response.Success();
    }

    public async Task<IResponse<bool>> AppendPluginToDatabase(LocalPlugin pluginData)
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");
        if (pluginDatabaseFile is null)
        {
            return Response.Failure("PluginDatabase file path is not present in the config file");
        }

        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();
        
        foreach (var dependency in pluginData.ListOfExecutableDependencies)
        {
            pluginData.ListOfExecutableDependencies[dependency.Key] = dependency.Value;
        }
        
        if (installedPlugins.Any(plugin => plugin.PluginName == pluginData.PluginName))
        {
            _Logger.Log($"Plugin {pluginData.PluginName} already exists in the database. Updating...", this, LogType.Info);
            installedPlugins.RemoveAll(p => p.PluginName == pluginData.PluginName);
        }

        installedPlugins.Add(pluginData);
        await JsonManager.SaveToJsonFile(pluginDatabaseFile, installedPlugins);
        
        return Response.Success();
    }

    public async Task<List<LocalPlugin>> GetInstalledPlugins()
    {
        string? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");
        if (pluginDatabaseFile is null)
        {
            _Logger.Log("Plugin database file path is not present in the config file", this, LogType.Warning);
            return [];
        }

        if (!File.Exists(pluginDatabaseFile))
        {
            _Logger.Log("Plugin database file not found", this, LogType.Warning);
            await CreateEmptyPluginDatabase();
            return [];
        }
        
        return await JsonManager.ConvertFromJson<List<LocalPlugin>>(await File.ReadAllTextAsync(pluginDatabaseFile));
    }

    public async Task<IResponse<string>> GetDependencyLocation(string dependencyName)
    {
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();

        foreach (var plugin in installedPlugins)
        {
            if (plugin.ListOfExecutableDependencies.TryGetValue(dependencyName, out var dependencyPath))
            {
                string relativePath = GenerateDependencyRelativePath(plugin.PluginName, dependencyPath);
                return Response<string>.Success(relativePath);
            }
        }

        return Response<string>.Failure($"Dependency {dependencyName} not found in the installed plugins.");
    }
    
    public async Task<IResponse<string>> GetDependencyLocation(string dependencyName, string pluginName)
    {
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();

        foreach (var plugin in installedPlugins)
        {
            if (plugin.PluginName == pluginName && plugin.ListOfExecutableDependencies.ContainsKey(dependencyName))
            {
                string dependencyPath     = plugin.ListOfExecutableDependencies[dependencyName];
                string relativePath = GenerateDependencyRelativePath(pluginName, dependencyPath); 
                return Response<string>.Success(relativePath);
            }
        }

        return Response<string>.Failure($"Dependency {dependencyName} not found in the installed plugins.");
    }

    public string GenerateDependencyRelativePath(string pluginName, string dependencyPath)
    {
        string relative = $"./{_LibrariesBaseFolder}/{pluginName}/{dependencyPath}";
        return relative;
    }

    public async Task<IResponse<bool>> InstallPlugin(OnlinePlugin plugin, IProgress<float> progress)
    {
        string? pluginsFolder = _Configuration.Get<string>("PluginFolder");
        if (pluginsFolder is null)
        {
            return Response.Failure("Plugin folder path is not present in the config file");
        }

        var localPluginResponse = await GetLocalPluginByName(plugin.Name);
        if (localPluginResponse is { IsSuccess: true, Data: not null })
        {
            var response = await IsNewVersion(localPluginResponse.Data.PluginVersion, plugin.Version);
            if (!response.IsSuccess)
            {
                return response;
            }
        }
        
        List<OnlineDependencyInfo> dependencies = await _PluginRepository.GetDependenciesForPlugin(plugin.Id);
        
        string downloadLocation = $"{pluginsFolder}/{plugin.Name}.dll";
        
        IProgress<float> downloadProgress = new Progress<float>(progress.Report);
        
        FileDownloader fileDownloader = new FileDownloader(plugin.DownloadLink, downloadLocation);
        await fileDownloader.DownloadFile(downloadProgress.Report);

        ParallelDownloadExecutor executor = new ParallelDownloadExecutor();

        foreach (var dependency in dependencies)
        {
            string dependencyLocation = GenerateDependencyRelativePath(plugin.Name, dependency.DownloadLocation);
            
            executor.AddTask(dependency.DownloadLink, dependencyLocation, progress.Report);
        }
        
        await executor.ExecuteAllTasks();
        
        LocalPlugin localPlugin = LocalPlugin.FromOnlineInfo(plugin, dependencies, downloadLocation);
        var result = await AppendPluginToDatabase(localPlugin);

        return result;
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

    public async Task<IResponse<bool>> UninstallPluginByName(string pluginName)
    {
        var localPluginResponse = await GetLocalPluginByName(pluginName);
        if (!localPluginResponse.IsSuccess)
        {
            return Response.Failure(localPluginResponse.Message);
        }

        var localPlugin = localPluginResponse.Data;
        
        if (localPlugin is null)
        {
            return Response.Failure($"Plugin {pluginName} not found in the database");
        }
        
        File.Delete(localPlugin.FilePath);

        if (Directory.Exists($"./{_LibrariesBaseFolder}/{pluginName}"))
        {
            foreach (var file in Directory.EnumerateFiles($"./{_LibrariesBaseFolder}/{pluginName}"))
            {
                File.Delete(file);
            }
        }
        
        var response = await RemovePluginFromDatabase(pluginName);
        return response;
    }

    public async Task<IResponse<LocalPlugin>> GetLocalPluginByName(string pluginName)
    {
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();
        var plugin = installedPlugins.Find(p => p.PluginName == pluginName);

        if (plugin is null)
        {
            return Response<LocalPlugin>.Failure($"Plugin {pluginName} not found in the database");
        }

        return Response<LocalPlugin>.Success(plugin);
    }

    private async Task<IResponse<bool>> IsNewVersion(string currentVersion, string newVersion)
    {
        // currentVersion = "1.0.0"
        // newVersion = "1.0.1"
        
        var currentVersionParts = currentVersion.Split('.').Select(int.Parse).ToArray();
        var newVersionParts = newVersion.Split('.').Select(int.Parse).ToArray();
        
        if (currentVersionParts.Length != 3 || newVersionParts.Length != 3)
        {
            return Response.Failure("Invalid version format");
        }
        
        for (int i = 0; i < 3; i++)
        {
            if (newVersionParts[i] > currentVersionParts[i])
            {
                return Response.Success();
            }
            else if (newVersionParts[i] < currentVersionParts[i])
            {
                return Response.Failure("Current version is newer");
            }
        }
        
        return Response.Failure("Versions are the same");
    }

    private async Task<bool> CreateEmptyPluginDatabase()
    {
        string ? pluginDatabaseFile = _Configuration.Get<string>("PluginDatabase");
        if (pluginDatabaseFile is null)
        {
            _Logger.Log("Plugin database file path is not present in the config file", this, LogType.Warning);
            return false;
        }
        
        if (File.Exists(pluginDatabaseFile))
        {
            _Logger.Log("Plugin database file already exists", this, LogType.Warning);
            return false;
        }
        
        List<LocalPlugin> installedPlugins = new List<LocalPlugin>();
        await JsonManager.SaveToJsonFile(pluginDatabaseFile, installedPlugins);
        _Logger.Log("Plugin database file created", this, LogType.Info);
        return true;
    }
}
