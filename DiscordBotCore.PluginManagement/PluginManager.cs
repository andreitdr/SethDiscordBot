using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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
    private readonly HttpClient _HttpClient;
    

    public PluginManager(IPluginRepository pluginRepository, ILocalPluginRepository localPluginRepository, ILogger logger, IConfiguration configuration)
        : this(pluginRepository, localPluginRepository, logger, configuration, new HttpClient())
    {
    }

    /// <param name="httpClient">Downloads plugin and dependency files.</param>
    public PluginManager(IPluginRepository pluginRepository, ILocalPluginRepository localPluginRepository, ILogger logger, IConfiguration configuration, HttpClient httpClient)
    {
        _HttpClient = httpClient;
        _PluginRepository = pluginRepository;
        _Logger = logger;
        _Configuration = configuration;
        _LocalPluginRepository = localPluginRepository;
    }

    public bool IsRepositoryConfigured => _PluginRepository.IsConfigured;

    public async Task<List<OnlinePlugin>> GetOnlinePluginsList()
    {
        if (!IsRepositoryConfigured)
        {
            return [];
        }

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
        if (!IsRepositoryConfigured)
        {
            return Response<OnlinePlugin>.Failure("No plugin repository is configured.");
        }

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
        if (!IsRepositoryConfigured)
        {
            return Response<OnlinePlugin>.Failure("No plugin repository is configured.");
        }

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
        if (!IsRepositoryConfigured)
        {
            return Response.Failure("No plugin repository is configured.");
        }

        string? pluginsFolder = _Configuration.Get<string>("PluginFolder");
        if (pluginsFolder is null)
        {
            return Response.Failure("Plugin folder path is not present in the config file");
        }

        // A plugin that is already installed is only replaced by a newer version
        var installedPlugin = await _LocalPluginRepository.GetPluginByNameAsync(plugin.Name);
        if (installedPlugin is not null)
        {
            var response = IsNewVersion(installedPlugin.PluginVersion, plugin.Version);
            if (!response.IsSuccess)
            {
                return response;
            }
        }
        
        // The name and the dependency locations come from the repository and become file paths
        string? librariesFolder = GetPluginLibrariesFolder(_LibrariesBaseFolder, plugin.Name);
        if (librariesFolder is null || plugin.Name.IndexOfAny(['/', '\\']) >= 0)
        {
            return Response.Failure($"The plugin name {plugin.Name} can't be used as a file name.");
        }

        List<OnlineDependencyInfo> dependencies = await _PluginRepository.GetDependenciesForPlugin(plugin.Id);
        if (dependencies.FirstOrDefault(d => GetPluginLibrariesFolder(librariesFolder, d.DownloadLocation) is null) is { } unsafeDependency)
        {
            return Response.Failure($"The dependency {unsafeDependency.DependencyName} would be saved outside the plugin's folder ({unsafeDependency.DownloadLocation}).");
        }
        
        string downloadLocation = $"{pluginsFolder}/{plugin.Name}.dll";
        
        try
        {
            FileDownloader fileDownloader = new FileDownloader(plugin.DownloadLink, downloadLocation, _HttpClient);
            await fileDownloader.DownloadFile(progress.Report);

            ParallelDownloadExecutor executor = new ParallelDownloadExecutor(_HttpClient);
            foreach (var dependency in dependencies)
            {
                string dependencyLocation = GenerateDependencyRelativePath(plugin.Name, dependency.DownloadLocation);
                executor.AddTask(dependency.DownloadLink, dependencyLocation, progress.Report);
            }
            
            await executor.ExecuteAllTasks();
        }
        catch (HttpRequestException ex)
        {
            _Logger.Log($"Downloading {plugin.Name} failed: {ex.Message}", this, LogType.Error);
            return Response.Failure($"Download failed: {ex.Message}");
        }
        
        LocalPlugin localPlugin = new LocalPlugin()
        {
            PluginName = plugin.Name,
            PluginVersion = plugin.Version,
            FilePath = downloadLocation,
            IsOfflineAdded = false,
            IsEnabled = installedPlugin?.IsEnabled ?? true
        };

        if (installedPlugin is not null)
        {
            var removeResponse = await RemovePluginFromDatabase(installedPlugin.PluginName);
            if (!removeResponse.IsSuccess)
            {
                return removeResponse;
            }
        }
        
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

        string? librariesFolder = GetPluginLibrariesFolder(_LibrariesBaseFolder, pluginName);
        if (librariesFolder is null)
        {
            _Logger.Log($"Plugin name {pluginName} doesn't map to a folder inside {_LibrariesBaseFolder}. Its libraries were not deleted.", this, LogType.Warning);
        }
        else if (Directory.Exists(librariesFolder))
        {
            try
            {
                Directory.Delete(librariesFolder, true);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _Logger.Log($"Couldn't delete the libraries of {pluginName}: {ex.Message}", this, LogType.Warning);
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

    /// <summary>
    /// The plugin's folder under <paramref name="librariesBaseFolder"/>, or null when the name would point outside it
    /// (empty, ".", "..", rooted paths and the like).
    /// </summary>
    internal static string? GetPluginLibrariesFolder(string librariesBaseFolder, string pluginName)
    {
        string librariesRoot = Path.GetFullPath(librariesBaseFolder);
        string pluginFolder = Path.GetFullPath(Path.Combine(librariesRoot, pluginName));

        string rootWithSeparator = Path.EndsInDirectorySeparator(librariesRoot) ? librariesRoot : librariesRoot + Path.DirectorySeparatorChar;
        if (!pluginFolder.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            return null;
        }

        return pluginFolder;
    }

    internal static IResponse<bool> IsNewVersion(string currentVersion, string newVersion)
    {
        // currentVersion = "1.0.0"
        // newVersion = "1.0.1"

        if (!TryParseVersion(currentVersion, out Version? current))
        {
            return Response.Failure($"Invalid installed version: {currentVersion}");
        }

        if (!TryParseVersion(newVersion, out Version? next))
        {
            return Response.Failure($"Invalid version: {newVersion}");
        }

        int comparison = next.CompareTo(current);
        if (comparison > 0)
        {
            return Response.Success();
        }

        return Response.Failure(comparison < 0 ? "Current version is newer" : "Versions are the same");
    }

    private static bool TryParseVersion(string? version, [NotNullWhen(true)] out Version? parsed)
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(version) || version.Split('.').Length != 3)
        {
            return false;
        }

        return Version.TryParse(version, out parsed);
    }
}
