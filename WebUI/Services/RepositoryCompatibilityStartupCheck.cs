using DiscordBotCore.PluginManagement;

namespace WebUI.Services;

/// <summary>Asks the plugin repository at startup, so a version mismatch shows up in the console straight away.</summary>
public sealed class RepositoryCompatibilityStartupCheck : BackgroundService
{
    private readonly IPluginManager _PluginManager;

    public RepositoryCompatibilityStartupCheck(IPluginManager pluginManager)
    {
        _PluginManager = pluginManager;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_PluginManager.IsRepositoryConfigured)
        {
            // The repository client logs anything other than compatible
            await _PluginManager.CheckRepositoryCompatibility(recheck: true);
        }
    }
}
