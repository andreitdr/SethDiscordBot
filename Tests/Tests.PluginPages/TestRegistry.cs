using Microsoft.Extensions.DependencyInjection;
using PluginPages;

namespace Tests.PluginPages;

internal static class TestRegistry
{
    public static ServiceProvider CreateServices(bool withRazorComponents = false)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        if (withRazorComponents)
        {
            services.AddSingleton<Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger, ThrowingErrorBoundaryLogger>();
        }

        services.AddPluginPages();
        return services.BuildServiceProvider();
    }

    public static PluginPageRegistry Create() => (PluginPageRegistry)CreateServices().GetRequiredService<IPluginPageRegistry>();

    public static PluginPageRegistrationResult Register(this PluginPageRegistry registry, string key, params Type[] types) =>
        registry.Register(key, key + " plugin", typeof(TestRegistry).Assembly, types);
}

/// <summary>Plugin page failures surface as test failures instead of an error boundary's fallback content.</summary>
internal sealed class ThrowingErrorBoundaryLogger : Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger
{
    public ValueTask LogErrorAsync(Exception exception) => ValueTask.FromException(exception);
}
