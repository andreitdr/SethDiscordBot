using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using DiscordBotCore.Others;
using DiscordBotCore.Plugin;
using DiscordBotCore.Repository;
using DiscordBotCore.Updater.Plugins;

namespace DiscordBotCore.Online;

public sealed class PluginManager
{
    private static readonly string _LibrariesBaseFolder = "Libraries";
    private readonly PluginRepository _PluginRepository;
    internal InstallingPluginInformation? InstallingPluginInformation { get; private set; }
    
    public PluginManager(PluginRepository pluginRepository)
    {
        _PluginRepository = pluginRepository;
    }

    public async Task<List<PluginOnlineInfo>> GetPluginsList()
    {
        var jsonText = await _PluginRepository.JsonGetAllPlugins();
        List<PluginOnlineInfo> result   = await JsonManager.ConvertFromJson<List<PluginOnlineInfo>>(jsonText);

        var currentOs = OperatingSystem.IsWindows() ? OSType.WINDOWS :
                        OperatingSystem.IsLinux() ? OSType.LINUX :
                        OperatingSystem.IsMacOS() ? OSType.MACOSX : OSType.NONE;

        return result.FindAll(pl => (pl.SupportedOS & currentOs) != 0);
    }

    public async Task<PluginOnlineInfo?> GetPluginDataByName(string pluginName)
    {
        List<PluginOnlineInfo>? plugins = await GetPluginsList();

        if (plugins is null)
        {
            return null;
        }
        
        PluginOnlineInfo? result = plugins.Find(pl => pl.Name.Contains(pluginName, StringComparison.CurrentCultureIgnoreCase));
        if (result is null)
        {
            return null;
        }

        return result;
    }

    public async Task RemovePluginFromDatabase(string pluginName)
    {
        List<PluginInfo> installedPlugins = await JsonManager.ConvertFromJson<List<PluginInfo>>(await File.ReadAllTextAsync(Application.CurrentApplication.PluginDatabase));

        installedPlugins.RemoveAll(p => p.PluginName == pluginName);
        await JsonManager.SaveToJsonFile(Application.CurrentApplication.PluginDatabase, installedPlugins);
    }

    public async Task ExecutePluginInstallScripts(List<OnlineScriptDependencyInfo> listOfDependencies)
    {
        string? console = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("console.terminal");
        if (string.IsNullOrEmpty(console))
        {
            return;
        }
        
        string? cmd_prefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("console.cmd_prefix");
        if (string.IsNullOrEmpty(cmd_prefix))
        {
            return;
        }
        
        foreach (var script in listOfDependencies)
            await ServerCom.RunConsoleCommand(console, $"{cmd_prefix}{script.ScriptContent}");
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

    public async Task CheckForUpdates()
    {
        var pluginUpdater = new PluginUpdater(this);

        List<PluginInfo> installedPlugins = await GetInstalledPlugins();

        foreach (var plugin in installedPlugins)
        {
            if (await pluginUpdater.HasUpdate(plugin.PluginName))
            {
                Application.CurrentApplication.Logger.Log("Updating plugin: " + plugin.PluginName, this, LogType.Info);
                await pluginUpdater.UpdatePlugin(plugin.PluginName);
            }
        }
    }

    public async Task<bool> MarkPluginToUninstall(string pluginName)
    {
        IEnumerable<PluginInfo> installedPlugins = await GetInstalledPlugins();
        IEnumerable<PluginInfo> info = installedPlugins.Where(info => info.PluginName == pluginName).AsEnumerable();

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

    public async Task InstallPluginWithNoProgress(PluginOnlineInfo pluginData)
    {
        InstallingPluginInformation = new InstallingPluginInformation() { PluginName = pluginData.Name };

        // Calculate the total number of steps: main file + dependencies
        int totalSteps = pluginData.HasFileDependencies ? pluginData.Dependencies.Count + 1 : 1;

        // Each step contributes this percentage to the total progress
        float stepFraction = 100f / totalSteps;

        // Tracks the current cumulative progress in percentage
        float currentProgress = 0f;

        InstallingPluginInformation.IsInstalling = true;

        // Create a progress updater that maps the file's 0–100 progress to its portion of the total progress
        IProgress<float> downloadProgress = new Progress<float>(fileProgress =>
        {
            // Map the file progress (0-100) to the total progress
            InstallingPluginInformation.InstallationProgress = currentProgress + (fileProgress / 100f) * stepFraction;
        });

        // Download the main plugin file and map its progress
        await ServerCom.DownloadFileAsync(pluginData.DownLoadLink,
            $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{pluginData.Name}.dll",
            downloadProgress
        );

        // Update cumulative progress after the main file
        currentProgress += stepFraction;

        // Download file dependencies if they exist
        if (pluginData.HasFileDependencies)
        {
            foreach (var dependency in pluginData.Dependencies)
            {
                string dependencyLocation =
                    GenerateDependencyRelativePath(pluginData.Name, dependency.DownloadLocation);

                // Download dependency and map its progress
                await ServerCom.DownloadFileAsync(dependency.DownloadLink, dependencyLocation, downloadProgress);

                // Update cumulative progress after each dependency
                currentProgress += stepFraction;
            }
        }

        // Run script dependencies if any (doesn't affect download progress percentage)
        if (pluginData.HasScriptDependencies)
        {
            foreach (var scriptDependency in pluginData.ScriptDependencies)
            {
                string? console = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("console.terminal");
                if (string.IsNullOrEmpty(console))
                {
                    return;
                }
                
                string? cmdPrefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("console.cmd_prefix");
                if (string.IsNullOrEmpty(cmdPrefix))
                {
                    return;
                }

                string arguments = $"{cmdPrefix}{scriptDependency.ScriptContent}";
                await ServerCom.RunConsoleCommand(console, arguments);
            }
        }

        // Register the plugin in the database
        PluginInfo pluginInfo = PluginInfo.FromOnlineInfo(pluginData);
        await AppendPluginToDatabase(pluginInfo);

        InstallingPluginInformation.IsInstalling = false; // Mark installation as complete
    }




    public async Task InstallPluginWithProgressBar(PluginOnlineInfo pluginData, IProgress<float>? installProgress)
    {
        installProgress?.Report(0f);

        int totalSteps = pluginData.HasFileDependencies ? pluginData.Dependencies.Count + 1 : 1;
        
        float stepProgress = 1f / totalSteps;

        float currentProgress = 0f;

        IProgress<float> progress = new Progress<float>((p) =>
        {
            installProgress?.Report(currentProgress + stepProgress * p);
        });

        await ServerCom.DownloadFileAsync(pluginData.DownLoadLink, $"{Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("PluginFolder")}/{pluginData.Name}.dll", progress);

        if (pluginData.HasFileDependencies)
            foreach (var dependency in pluginData.Dependencies)
            {
                string dependencyLocation = GenerateDependencyRelativePath(pluginData.Name, dependency.DownloadLocation);
                await ServerCom.DownloadFileAsync(dependency.DownloadLink, dependencyLocation, progress);

                currentProgress += stepProgress;
            }

        if (pluginData.HasScriptDependencies)
        {
            foreach (var scriptDependency in pluginData.ScriptDependencies)
            {
                string? console = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("console.terminal");
                if (string.IsNullOrEmpty(console))
                {
                    return;
                }
                
                string? cmdPrefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("console.cmd_prefix");
                if (string.IsNullOrEmpty(cmdPrefix))
                {
                    return;
                }

                string arguments = $"{cmdPrefix}{scriptDependency.ScriptContent}";

                await ServerCom.RunConsoleCommand(console, arguments);
            }
            
        }

        PluginInfo pluginInfo = PluginInfo.FromOnlineInfo(pluginData);

        await AppendPluginToDatabase(pluginInfo);
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
