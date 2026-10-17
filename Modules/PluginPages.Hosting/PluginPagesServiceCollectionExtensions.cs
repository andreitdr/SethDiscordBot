using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace PluginPages;

public static class PluginPagesServiceCollectionExtensions
{
    /// <summary>
    /// Registers the plugin page registry. The host still needs a routable page that renders <see cref="PluginPageOutlet"/>
    /// at <c>{BasePath}/{PluginKey}/{*PluginPath}</c>, and <see cref="PluginPageAssetEndpoints.MapPluginPageAssets"/> for embedded files.
    /// </summary>
    public static IServiceCollection AddPluginPages(this IServiceCollection services, Action<PluginPagesOptions>? configure = null)
    {
        services.AddOptions<PluginPagesOptions>();
        if (configure is not null)
        {
            services.Configure(configure);
        }

        services.AddRouting();
        services.TryAddSingleton<IPluginPageRegistry, PluginPageRegistry>();
        return services;
    }
}
