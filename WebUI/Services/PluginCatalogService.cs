using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Resources;
using DiscordBotCore.Utilities.Responses;
using IConfiguration = DiscordBotCore.Configuration.IConfiguration;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Services;

/// <summary>
/// Cached view of the installed and online plugins, plus every action that changes them.
/// Install progress lives here too, so a download started in one tab is visible in all of them.
/// </summary>
public sealed partial class PluginCatalogService : IStateNotifier
{
    public const long MaxLocalPluginSize = 10 * 1024 * 1024;

    private readonly IPluginManager _PluginManager;
    private readonly BotStateService _BotState;
    private readonly IConfiguration _Configuration;
    private readonly ILogger _Logger;

    private readonly ConcurrentDictionary<int, float> _InstallProgress = new();
    private readonly SemaphoreSlim _OnlineGate = new(1, 1);

    public event Action? Changed;

    public PluginCatalogService(IPluginManager pluginManager, BotStateService botState, IConfiguration configuration, ILogger logger)
    {
        _PluginManager = pluginManager;
        _BotState = botState;
        _Configuration = configuration;
        _Logger = logger;
    }

    public IReadOnlyList<LocalPlugin> Installed { get; private set; } = [];
    public bool InstalledLoaded { get; private set; }

    public IReadOnlyList<OnlinePlugin> Online { get; private set; } = [];
    public bool OnlineLoaded { get; private set; }
    public bool OnlineLoading { get; private set; }
    public string? OnlineError { get; private set; }

    public bool IsInstalled(string pluginName) =>
        Installed.Any(p => string.Equals(p.PluginName, pluginName, StringComparison.OrdinalIgnoreCase));

    /// <summary>Download progress (0-100) for plugins that are being installed right now.</summary>
    public bool TryGetInstallProgress(int onlinePluginId, out float percent) =>
        _InstallProgress.TryGetValue(onlinePluginId, out percent);

    public async Task EnsureInstalledLoadedAsync()
    {
        if (!InstalledLoaded)
        {
            await RefreshInstalledAsync();
        }
    }

    public async Task RefreshInstalledAsync()
    {
        try
        {
            Installed = await _PluginManager.GetInstalledPlugins();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            Installed = [];
        }

        InstalledLoaded = true;
        Changed?.Invoke();
    }

    public async Task EnsureOnlineLoadedAsync()
    {
        if (!OnlineLoaded && !OnlineLoading)
        {
            await RefreshOnlineAsync();
        }
    }

    public async Task RefreshOnlineAsync()
    {
        if (!await _OnlineGate.WaitAsync(0))
        {
            return;
        }

        try
        {
            OnlineLoading = true;
            OnlineError = null;
            Changed?.Invoke();

            Online = await _PluginManager.GetOnlinePluginsList();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            OnlineError = $"Couldn't reach the plugin repository: {ex.Message}";
        }
        finally
        {
            OnlineLoading = false;
            OnlineLoaded = true;
            _OnlineGate.Release();
            Changed?.Invoke();
        }
    }

    public async Task<IResponse<bool>> InstallAsync(OnlinePlugin plugin)
    {
        if (!_InstallProgress.TryAdd(plugin.Id, 0f))
        {
            return Response.Failure($"{plugin.Name} is already being installed.");
        }

        Changed?.Invoke();
        try
        {
            var details = await _PluginManager.GetOnlinePluginDataById(plugin.Id);
            if (!details.IsSuccess || details.Data is null)
            {
                return Response.Failure(string.IsNullOrEmpty(details.Message) ? "The plugin repository didn't return this plugin." : details.Message);
            }

            _Logger.Log($"Installing plugin {details.Data.Name}...", this);
            var progress = new Progress<float>(percent =>
            {
                _InstallProgress[plugin.Id] = percent;
                Changed?.Invoke();
            });

            var result = await _PluginManager.InstallPlugin(details.Data, progress);
            if (!result.IsSuccess)
            {
                return Response.Failure(result.Message);
            }

            _Logger.Log($"Plugin {details.Data.Name} installed successfully.", this);
            return Response.Success();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            return Response.Failure(ex.Message);
        }
        finally
        {
            _InstallProgress.TryRemove(plugin.Id, out _);
            await RefreshInstalledAsync();
        }
    }

    public async Task<IResponse<bool>> UninstallAsync(string pluginName)
    {
        var result = await _BotState.WithPluginsUnloadedAsync(async () =>
        {
            try
            {
                _Logger.Log($"Deleting plugin {pluginName}", this);
                var response = await _PluginManager.UninstallPluginByName(pluginName);
                if (!response.IsSuccess)
                {
                    _Logger.Log(response.Message, this, LogType.Warning);
                    return Response.Failure(response.Message);
                }

                _Logger.Log($"Plugin {pluginName} deleted", this);
                return Response.Success();
            }
            catch (Exception ex)
            {
                _Logger.LogException(ex, this);
                return Response.Failure($"Deleting {pluginName} failed: {ex.Message}");
            }
        });

        await RefreshInstalledAsync();
        return result;
    }

    public async Task<IResponse<bool>> SetEnabledAsync(string pluginName, bool isEnabled)
    {
        try
        {
            await _PluginManager.SetEnabledStatus(pluginName, isEnabled);
            _Logger.Log($"Plugin {pluginName} {(isEnabled ? "enabled" : "disabled")}", this);
            return Response.Success();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            return Response.Failure(ex.Message);
        }
        finally
        {
            await RefreshInstalledAsync();
        }
    }

    public async Task<IResponse<OnlinePlugin>> GetOnlineDetailsAsync(string pluginName)
    {
        try
        {
            return await _PluginManager.GetOnlinePluginDataByName(pluginName);
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            return Response<OnlinePlugin>.Failure($"Couldn't reach the plugin repository: {ex.Message}");
        }
    }

    public static bool IsValidVersion(string? version) => version is not null && VersionPattern().IsMatch(version);

    public async Task<IResponse<bool>> AddLocalAsync(string name, string version, bool isEnabled, string fileName, Stream content)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name))
        {
            return Response.Failure("The plugin needs a name.");
        }

        if (!IsValidVersion(version))
        {
            return Response.Failure("The version must look like 1.0.0.");
        }

        if (IsInstalled(name))
        {
            return Response.Failure($"A plugin named {name} is already installed.");
        }

        // Only keep the file name: it comes from the browser.
        string safeFileName = Path.GetFileName(fileName);
        if (!safeFileName.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
        {
            return Response.Failure("Only .dll files can be added.");
        }

        string pluginsFolder = _Configuration.Get<string>(DefaultConfigurationKeys.PluginFolder, DefaultConfigurationValues.DefaultPluginFolder);
        Directory.CreateDirectory(pluginsFolder);
        string filePath = Path.Combine(pluginsFolder, safeFileName);

        try
        {
            await using (var fileStream = File.Create(filePath))
            {
                await content.CopyToAsync(fileStream);
            }

            var plugin = await _PluginManager.CreateOfflineLocalPlugin(name, filePath, version, isEnabled);
            var appended = await _PluginManager.AppendPluginToDatabase(plugin, []);
            if (!appended.IsSuccess)
            {
                return Response.Failure(appended.Message);
            }

            _Logger.Log($"Plugin {name} added from {safeFileName}", this);
            return Response.Success();
        }
        catch (Exception ex)
        {
            _Logger.LogException(ex, this);
            return Response.Failure(ex.Message);
        }
        finally
        {
            await RefreshInstalledAsync();
        }
    }

    [GeneratedRegex(@"^\d+\.\d+\.\d+$")]
    private static partial Regex VersionPattern();
}
