using System.Reflection;

namespace PluginPages;

/// <summary>
/// The runtime set of plugin pages. Plugins are registered when their assemblies load and unregistered before their
/// load context unloads; every lookup sees a consistent snapshot.
/// </summary>
public interface IPluginPageRegistry
{
    /// <summary>Every accepted page of every registered plugin, ordered by plugin name, then <see cref="PluginPageAttribute.Order"/>.</summary>
    IReadOnlyList<PluginPageDescriptor> Pages { get; }

    /// <summary>Every page that was refused during registration, with the reason.</summary>
    IReadOnlyList<PluginPageRejection> Rejections { get; }

    /// <summary>Raised after any registration change, on the thread that made it.</summary>
    event Action? Changed;

    /// <summary>
    /// Scans <paramref name="assembly"/> for components marked with <see cref="PluginPageAttribute"/>, validates their
    /// routes and makes the accepted ones reachable under <c>{BasePath}/{pluginKey}</c>. Registering a key again replaces
    /// the previous registration.
    /// </summary>
    PluginPageRegistrationResult Register(string pluginKey, string pluginName, Assembly assembly);

    /// <summary>Removes one plugin's pages. Returns false when the key isn't registered.</summary>
    bool Unregister(string pluginKey);

    /// <summary>Removes every plugin's pages.</summary>
    void Clear();

    bool IsRegistered(string pluginKey);

    bool TryGetPluginName(string pluginKey, out string pluginName);

    /// <summary>Resolves <paramref name="relativePath"/> (relative to the plugin's prefix) to one of the plugin's pages.</summary>
    bool TryMatch(string pluginKey, string? relativePath, out PluginPageMatch match);

    /// <summary>Opens a file embedded in the plugin assembly under the assets root, for the asset endpoint.</summary>
    bool TryOpenAsset(string pluginKey, string path, out Stream stream);
}
