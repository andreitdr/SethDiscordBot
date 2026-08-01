using System.Diagnostics;
using DiscordBotCore.Logging;
using DiscordBotCore.Networking;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities;
using DiscordBotCore.Configuration;
using DiscordBotCore.Database.Sqlite;
using DiscordBotCore.Resources;
using DiscordBotCore.Utilities.Responses;
using OperatingSystem = DiscordBotCore.Utilities.OperatingSystem;

namespace DiscordBotCore.PluginManagement;

public sealed class PluginManager : IPluginManager
{
    private static readonly string _LibrariesBaseFolder = "Libraries";
    private readonly IPluginRepository _PluginRepository;
    private readonly ILocalPluginRepository _LocalPluginRepository;
    private readonly ILogger _Logger;
    private readonly IConfiguration _Configuration;
    

    public PluginManager(IPluginRepository pluginRepository, ILocalPluginRepository localPluginRepository, ILogger logger, IConfiguration configuration)
    {
        _PluginRepository = pluginRepository;
        _Logger = logger;
        _Configuration = configuration;
        _LocalPluginRepository = localPluginRepository;
    }

    public async Task<List<OnlinePlugin>> GetOnlinePluginsList()
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

    public async Task<IResponse<OnlinePlugin>> GetOnlinePluginDataByName(string pluginName)
    {
        int os = OperatingSystem.GetOperatingSystemInt();
        var plugin = await _PluginRepository.GetPluginByName(pluginName, os, false);

        if (plugin is null)
        {
            return Response<OnlinePlugin>.Failure($"Plugin {pluginName} not found in the repository for operating system {OperatingSystem.GetOperatingSystemString((OperatingSystem.OperatingSystemEnum)os)}.");
        }

        return Response<OnlinePlugin>.Success(plugin);
    }

    public async Task<IResponse<OnlinePlugin>> GetOnlinePluginDataById(int pluginId)
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
        var plugin = await _LocalPluginRepository.GetPluginByNameAsync(pluginName);

        if (plugin is null)
        {
            return Response.Failure($"Plugin {pluginName} not found in the repository.");
        }
        
        await _LocalPluginRepository.DeletePluginAsync(plugin.Id);
        await _LocalPluginRepository.DeleteAllDependenciesForPluginAsync(plugin.Id);
        
        return Response.Success();
    }

    public async Task<IResponse<bool>> AppendPluginToDatabase(LocalPlugin pluginData, List<LocalDependencyInfo> dependencies)
    {
        List<LocalPlugin> installedPlugins = await GetInstalledPlugins();
        
        if(installedPlugins.Any(plugin => plugin.PluginName == pluginData.PluginName && plugin.PluginVersion == pluginData.PluginVersion))
        {
            _Logger.Log($"Plugin {pluginData.PluginName} version {pluginData.PluginVersion} already exists in the database. Skipping ...", this, LogType.Info);
            return Response.Success();
        }

        try
        {
            var guid = await _LocalPluginRepository.AddPluginAsync(pluginData);
            foreach (var dependency in dependencies)
            {
                dependency.PluginId = guid;
                await _LocalPluginRepository.AddDependencyAsync(dependency);
            }
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this, true);
            return Response.Failure(ex.Message);
        }
        
        return Response.Success();
    }

    public async Task<List<LocalPlugin>> GetInstalledPlugins()
    {
        return await _LocalPluginRepository.GetAllPluginsAsync();
    }
    
    public async Task<IResponse<string>> GetDependencyLocation(string dependencyName, string pluginName)
    {
        var dependencies = await _LocalPluginRepository.GetDependenciesForPluginAsync(pluginName);
        var searchedDependency = dependencies.FirstOrDefault(d => d.DependencyName == dependencyName);
        if (searchedDependency is null)
        {
            return Response<string>.Failure($"Dependency {dependencyName} not found in the repository.");
        }
        
        // DependencyLocation: folder/myExe.exe
        // RelativePath: "./Libraries/PluginName/folder/myExe.exe"
        
        return Response<string>.Success(GenerateDependencyRelativePath(pluginName, searchedDependency.DependencyLocation));
    }

    private string GenerateDependencyRelativePath(string pluginName, string dependencyPath)
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

        var localPluginResponse = await _LocalPluginRepository.GetPluginByNameAsync(plugin.Name);
        if (localPluginResponse is null)
        {
            return Response.Failure($"Plugin {plugin.Name} not found in the repository");
        }
        
        var response = await IsNewVersion(localPluginResponse.PluginVersion, plugin.Version);
        if (!response.IsSuccess)
        {
            return response;
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
        
        LocalPlugin localPlugin = new LocalPlugin()
        {
            PluginName = plugin.Name,
            PluginVersion = plugin.Version,
            FilePath = downloadLocation,
            IsOfflineAdded = false,
            IsEnabled = true
        };
        
        var pluginId = await _LocalPluginRepository.AddPluginAsync(localPlugin);

        foreach (var executable in dependencies.Where(d => d.IsExecutable))
        {
            LocalDependencyInfo localDependencyInfo = new LocalDependencyInfo()
            {
                DependencyName = executable.DependencyName,
                DependencyLocation = executable.DownloadLocation,
                PluginId = pluginId,
            };
            await _LocalPluginRepository.AddDependencyAsync(localDependencyInfo);
        }
        
        return Response.Success();
    }

    public async Task SetEnabledStatus(string pluginName, bool status)
    {
        var plugin = await _LocalPluginRepository.GetPluginByNameAsync(pluginName);
        if (plugin is null)
        {
            return;
        }

        plugin.IsEnabled = status;
        
        await _LocalPluginRepository.UpdatePluginAsync(plugin);

    }

    public async Task<IResponse<bool>> UninstallPluginByName(string pluginName)
    {
        var localPlugin = await _LocalPluginRepository.GetPluginByNameAsync(pluginName);
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

    public Task<LocalPlugin> CreateOfflineLocalPlugin(string pluginName, string location, string version, bool isEnabled)
    {
        LocalPlugin plugin = new LocalPlugin()
        {
            Id = Guid.CreateVersion7(),
            PluginName = pluginName,
            PluginVersion = version,
            FilePath = location,
            IsEnabled = isEnabled,
            IsOfflineAdded = true,
        };

        return Task.FromResult<LocalPlugin>(plugin);
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
}
