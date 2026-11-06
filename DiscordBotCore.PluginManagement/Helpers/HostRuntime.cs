using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace DiscordBotCore.PluginManagement.Helpers;

/// <summary>What the running bot itself provides, and where a plugin's own libraries live.</summary>
public static class HostRuntime
{
    public const string LibrariesFolderName = "Libraries";

    private static readonly Lazy<Dictionary<string, string>> _PlatformAssemblies = new(() =>
    {
        var assemblies = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string paths = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string ?? "";
        foreach (string path in paths.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
        {
            assemblies[Path.GetFileNameWithoutExtension(path)] = path;
        }

        return assemblies;
    });

    /// <summary>
    /// True for an assembly the bot ships. Plugins always share the bot's copy, so that types such as IDbCommand and
    /// the Discord.Net ones stay the same types on both sides.
    /// </summary>
    public static bool ProvidesAssembly(string assemblyName) =>
        _PlatformAssemblies.Value.ContainsKey(assemblyName) ||
        AssemblyLoadContext.Default.Assemblies.Any(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));

    /// <summary>The version of an assembly the bot ships; null when it doesn't ship it.</summary>
    public static Version? GetProvidedVersion(string assemblyName)
    {
        Assembly? loaded = AssemblyLoadContext.Default.Assemblies.FirstOrDefault(assembly => string.Equals(assembly.GetName().Name, assemblyName, StringComparison.OrdinalIgnoreCase));
        if (loaded is not null)
        {
            return loaded.GetName().Version;
        }

        return _PlatformAssemblies.Value.TryGetValue(assemblyName, out string? path) ? AssemblyName.GetAssemblyName(path).Version : null;
    }

    /// <summary>The current platform's runtime identifiers, most specific first, as used in NuGet's runtimes/ folders.</summary>
    public static IReadOnlyList<string> RuntimeIdentifiers { get; } = CreateRuntimeIdentifiers();

    private static List<string> CreateRuntimeIdentifiers()
    {
        string os = OperatingSystem.IsWindows() ? "win" : OperatingSystem.IsMacOS() ? "osx" : "linux";
        string architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

        var identifiers = new List<string> { RuntimeInformation.RuntimeIdentifier, $"{os}-{architecture}", os };
        if (!OperatingSystem.IsWindows())
        {
            identifiers.Add("unix");
        }

        return identifiers.Distinct().ToList();
    }

    /// <summary>
    /// Where a plugin's libraries can be: under the working directory, where the installer writes them, and next to
    /// the binaries. They are the same folder unless the bot runs with dotnet run. Empty for a name that would leave
    /// the libraries folder.
    /// </summary>
    public static IReadOnlyList<string> GetPluginLibraryFolders(string pluginName)
    {
        if (string.IsNullOrWhiteSpace(pluginName) || pluginName.IndexOfAny(['/', '\\']) >= 0 || pluginName is "." or "..")
        {
            return [];
        }

        return new[]
            {
                Path.GetFullPath(Path.Combine(LibrariesFolderName, pluginName)),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, LibrariesFolderName, pluginName))
            }
            .Distinct()
            .ToList();
    }
}
