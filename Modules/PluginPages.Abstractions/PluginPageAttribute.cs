namespace PluginPages;

/// <summary>
/// Marks a Razor component in a plugin assembly as a page the host mounts under the plugin's own prefix
/// (for example <c>/plugin/&lt;plugin id&gt;</c>). A component may carry several of these to answer on several routes.
/// </summary>
/// <remarks>
/// Use this instead of <c>@page</c>: the host never registers plugin assemblies with its router, so <c>@page</c> routes
/// in a plugin are ignored.
/// </remarks>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
public sealed class PluginPageAttribute : Attribute
{
    /// <param name="route">
    /// The route relative to the plugin's prefix, starting with <c>/</c>. <c>"/"</c> is the plugin's index page.
    /// ASP.NET Core route templates are supported, for example <c>"/users/{id:long}"</c> or <c>"/files/{*path}"</c>.
    /// </param>
    public PluginPageAttribute(string route)
    {
        Route = route;
    }

    public string Route { get; }

    /// <summary>The name shown in the host's menus and in the page title. Pages without one are not listed.</summary>
    public string? Title { get; set; }

    /// <summary>A Material Symbols icon name, such as <c>settings</c>.</summary>
    public string? Icon { get; set; }

    /// <summary>The position among the plugin's pages in menus; lower comes first.</summary>
    public int Order { get; set; }

    /// <summary>Keeps a titled page out of the host's menus. Pages with route parameters are never listed.</summary>
    public bool HideFromMenu { get; set; }
}
