using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Loading;
using PluginPages;
using ILogger = DiscordBotCore.Logging.ILogger;

namespace WebUI.Services;

/// <summary>
/// Keeps the plugin page registry in step with the plugin loader: pages of every loaded plugin are registered under the
/// plugin's id after a load and removed before the plugins' load context unloads.
/// </summary>
public sealed class PluginPagesBridge : IHostedService
{
    private static readonly TimeSpan SecondPurgeDelay = TimeSpan.FromSeconds(2);

    private readonly IPluginLoader _PluginLoader;
    private readonly IPluginPageRegistry _Registry;
    private readonly ILogger _Logger;

    public PluginPagesBridge(IPluginLoader pluginLoader, IPluginPageRegistry registry, ILogger logger)
    {
        _PluginLoader = pluginLoader;
        _Registry = registry;
        _Logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _PluginLoader.PluginsLoaded += OnPluginsLoaded;
        _PluginLoader.PluginsUnloading += OnPluginsUnloading;

        // The loader is a singleton, so plugins may already be loaded if it was resolved and used before the host started.
        if (_PluginLoader.IsLoaded)
        {
            OnPluginsLoaded();
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _PluginLoader.PluginsLoaded -= OnPluginsLoaded;
        _PluginLoader.PluginsUnloading -= OnPluginsUnloading;
        return Task.CompletedTask;
    }

    private void OnPluginsLoaded()
    {
        _Registry.Clear();

        foreach (LoadedPluginAssembly loaded in _PluginLoader.LoadedPlugins)
        {
            PluginPageRegistrationResult result = _Registry.Register(loaded.Plugin.Id.ToString("D"), loaded.Plugin.PluginName, loaded.Assembly);

            foreach (PluginPageDescriptor page in result.Accepted)
            {
                _Logger.Log($"Plugin page: {page.Href} ({page.Title ?? page.ComponentTypeName})", this);
            }

            foreach (PluginPageRejection rejection in result.Rejected)
            {
                string what = rejection.ComponentTypeName is null ? "" : $" {rejection.ComponentTypeName}";
                string route = rejection.Route is null ? "" : $" [{rejection.Route}]";
                _Logger.Log($"Plugin {rejection.PluginName}: page{what}{route} was rejected: {rejection.Reason}", this, LogType.Warning);
            }
        }
    }

    private void OnPluginsUnloading()
    {
        // Clearing makes every open plugin page re-render without the plugin component, and purges Blazor's type caches.
        _Registry.Clear();

        // Open circuits re-render asynchronously, so purge once more after they have let go of the plugin components.
        _ = Task.Delay(SecondPurgeDelay).ContinueWith(_ => PluginComponentCaches.TryPurge(), TaskScheduler.Default);
    }
}
