namespace PluginPages;

/// <summary>
/// Describes the plugin page being rendered. The host cascades it to every plugin page, so a page receives it with
/// <c>[CascadingParameter] public PluginPageContext Context { get; set; }</c>.
/// </summary>
public sealed class PluginPageContext
{
    private readonly string _AssetVersion;

    public PluginPageContext(
        string pluginKey,
        string pluginName,
        string basePath,
        string assetsSegment,
        string assetVersion,
        string relativePath,
        string? title,
        IReadOnlyDictionary<string, string?> routeValues)
    {
        PluginKey = pluginKey;
        PluginName = pluginName;
        BasePath = basePath.TrimEnd('/');
        AssetsPath = $"{BasePath}/{assetsSegment}";
        _AssetVersion = assetVersion;
        RelativePath = relativePath;
        Title = title;
        RouteValues = routeValues;
    }

    /// <summary>The key the host registered the plugin under (its id).</summary>
    public string PluginKey { get; }

    public string PluginName { get; }

    /// <summary>The absolute path every page of this plugin lives under, such as <c>/plugin/3f2c…</c>, without a trailing slash.</summary>
    public string BasePath { get; }

    /// <summary>The absolute path embedded assets are served from.</summary>
    public string AssetsPath { get; }

    /// <summary>The requested path relative to <see cref="BasePath"/>, always starting with <c>/</c>.</summary>
    public string RelativePath { get; }

    /// <summary>The <see cref="PluginPageAttribute.Title"/> of the matched route, if any.</summary>
    public string? Title { get; }

    /// <summary>The raw values captured by the route template, keyed by parameter name (case-insensitive).</summary>
    public IReadOnlyDictionary<string, string?> RouteValues { get; }

    /// <summary>
    /// The absolute URL of another page of this plugin. Use it for every link and <c>NavigationManager.NavigateTo</c>:
    /// the host sets <c>&lt;base href="/"&gt;</c>, so a plain relative link would resolve from the site root.
    /// </summary>
    public string Href(string relativePath = "/")
    {
        string trimmed = relativePath.TrimStart('/');
        return trimmed.Length == 0 ? BasePath : $"{BasePath}/{trimmed}";
    }

    /// <summary>
    /// The URL of a file embedded in the plugin under <c>wwwroot/</c>, versioned so a reinstalled plugin is never served
    /// from a stale browser cache. For a JavaScript module: <c>JS.InvokeAsync&lt;IJSObjectReference&gt;("import", Context.AssetUrl("app.js"))</c>.
    /// </summary>
    public string AssetUrl(string path) => $"{AssetsPath}/{path.TrimStart('/')}?v={_AssetVersion}";
}
