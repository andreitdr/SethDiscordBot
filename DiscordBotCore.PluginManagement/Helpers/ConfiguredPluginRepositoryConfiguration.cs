using DiscordBotCore.Configuration;
using DiscordBotCore.Resources;

namespace DiscordBotCore.PluginManagement.Helpers;

/// <summary>
/// Reads the repository details from the configuration every time, so a changed BaseUrl takes effect without a restart.
/// </summary>
public sealed class ConfiguredPluginRepositoryConfiguration : IPluginRepositoryConfiguration
{
    private readonly IConfiguration _configuration;

    public ConfiguredPluginRepositoryConfiguration(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    private PluginRepositoryConfiguration Current
    {
        get
        {
            PluginRepositoryConfiguration defaults = PluginRepositoryConfiguration.Default;
            Dictionary<string, string>? details = _configuration.Get<Dictionary<string, string>>(DefaultConfigurationKeys.RemotePluginConnectionConfiguration.Details);

            if (details is null ||
                !details.TryGetValue(DefaultConfigurationKeys.RemotePluginConnectionConfiguration.BaseUrl, out string? baseUrl) ||
                string.IsNullOrWhiteSpace(baseUrl))
            {
                return defaults;
            }

            return new PluginRepositoryConfiguration(
                baseUrl,
                details.GetValueOrDefault(DefaultConfigurationKeys.RemotePluginConnectionConfiguration.PluginsEndpoint, defaults.PluginRepositoryLocation),
                details.GetValueOrDefault(DefaultConfigurationKeys.RemotePluginConnectionConfiguration.DependenciesEndpoint, defaults.DependenciesRepositoryLocation));
        }
    }

    public string BaseUrl => Current.BaseUrl;
    public string PluginRepositoryLocation => Current.PluginRepositoryLocation;
    public string DependenciesRepositoryLocation => Current.DependenciesRepositoryLocation;
    public bool IsConfigured => Current.IsConfigured;
}
