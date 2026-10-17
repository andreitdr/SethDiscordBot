namespace PluginPages;

public sealed class PluginPagesOptions
{
    /// <summary>
    /// The path every plugin's pages live under, followed by the plugin key. It must match the host page's <c>@page</c>
    /// routes (<c>{BasePath}/{PluginKey}</c> and <c>{BasePath}/{PluginKey}/{*PluginPath}</c>).
    /// </summary>
    public string BasePath { get; set; } = "/plugin";

    /// <summary>The first path segment, under a plugin's prefix, that serves its embedded files. Plugins can't use it for pages.</summary>
    public string AssetsSegment { get; set; } = "_assets";

    /// <summary>The folder (logical resource name prefix) embedded files are looked up under.</summary>
    public string EmbeddedAssetsRoot { get; set; } = "wwwroot";

    /// <summary>Clears Blazor's static per-type caches after plugins are unregistered, so their load context can be collected.</summary>
    public bool PurgeComponentCachesOnUnregister { get; set; } = true;
}
