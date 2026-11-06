using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Helpers;

namespace DiscordBotCore.PluginManagement.Loading;

/// <summary>
/// The load context of one plugin. Everything the bot ships comes from the bot, so shared types stay the same types.
/// Everything else comes from the plugin's own libraries folder, so plugins can use different versions of a package.
/// </summary>
public class PluginLoaderContext : AssemblyLoadContext
{
    private readonly ILogger _logger;
    private readonly IReadOnlyList<string> _libraryFolders;
    private readonly NativeLibraryTracker _nativeLibraries;

    public PluginLoaderContext(ILogger logger, string name, IReadOnlyList<string> libraryFolders, NativeLibraryTracker nativeLibraries) : base(name: name, isCollectible: true)
    {
        _logger = logger;
        _libraryFolders = libraryFolders;
        _nativeLibraries = nativeLibraries;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        if (assemblyName.Name is null || HostRuntime.ProvidesAssembly(assemblyName.Name))
        {
            return null;
        }

        string? path = FindManagedLibrary(assemblyName);
        if (path is null)
        {
            return null;
        }

        try
        {
            return PluginLoader.LoadFromMemory(this, path);
        }
        catch (Exception exception) when (exception is BadImageFormatException or IOException)
        {
            _logger.Log($"Plugin {Name}: failed to load {path}: {exception.Message}", this, LogType.Error);
            return null;
        }
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        string? path = FindNativeLibrary(unmanagedDllName);
        if (path is null)
        {
            // Not one of the plugin's: let the runtime look in the usual places, such as for libc
            return IntPtr.Zero;
        }

        try
        {
            return _nativeLibraries.Load(path);
        }
        catch (Exception exception) when (exception is DllNotFoundException or BadImageFormatException or IOException)
        {
            _logger.Log($"Plugin {Name}: failed to load {path}: {exception.Message}", this, LogType.Error);
            return IntPtr.Zero;
        }
    }

    private string? FindManagedLibrary(AssemblyName assemblyName)
    {
        string fileName = assemblyName.Name + ".dll";
        string culture = assemblyName.CultureName ?? "";

        foreach (string folder in _libraryFolders)
        {
            string path = culture.Length > 0 ? Path.Combine(folder, culture, fileName) : Path.Combine(folder, fileName);
            if (File.Exists(path))
            {
                return path;
            }
        }

        return null;
    }

    private string? FindNativeLibrary(string name)
    {
        // A DllImport usually names the library without its prefix and extension
        string[] extensions = OperatingSystem.IsWindows() ? [".dll"] : OperatingSystem.IsMacOS() ? [".dylib", ".so"] : [".so"];
        var candidates = new List<string> { name };
        foreach (string extension in extensions)
        {
            candidates.Add(name + extension);
            candidates.Add("lib" + name + extension);
        }

        foreach (string folder in _libraryFolders)
        {
            IEnumerable<string> searched = new[] { folder }.Concat(HostRuntime.RuntimeIdentifiers.Select(rid => Path.Combine(folder, "runtimes", rid, "native")));
            foreach (string directory in searched)
            {
                foreach (string candidate in candidates)
                {
                    string path = Path.Combine(directory, candidate);
                    if (File.Exists(path))
                    {
                        return path;
                    }
                }
            }
        }

        return null;
    }
}

/// <summary>
/// The native libraries of one plugin load. .NET never frees native libraries when a load context unloads, so they are
/// recorded here and freed once the context is known to be gone. They load from a private copy, so the original file
/// stays unlocked and a new version loads from a new path, even if the OS still holds on to the old one.
/// </summary>
public sealed class NativeLibraryTracker
{
    private readonly string _cacheFolder;
    private readonly ILogger _logger;
    private readonly List<IntPtr> _handles = new();
    private readonly Lock _lock = new();

    public NativeLibraryTracker(string cacheRoot, ILogger logger)
    {
        _cacheFolder = Path.Combine(cacheRoot, Guid.NewGuid().ToString("N"));
        _logger = logger;
    }

    public IntPtr Load(string path)
    {
        lock (_lock)
        {
            string copy = CopyToCache(path);
            IntPtr handle = NativeLibrary.Load(copy);
            _handles.Add(handle);
            return handle;
        }
    }

    /// <summary>Only call this once nothing can run the plugin's code anymore, or it would crash the process.</summary>
    public void FreeAll()
    {
        lock (_lock)
        {
            foreach (IntPtr handle in Enumerable.Reverse(_handles))
            {
                NativeLibrary.Free(handle);
            }

            _handles.Clear();
        }

        DeleteCopies();
    }

    public void DeleteCopies()
    {
        try
        {
            if (Directory.Exists(_cacheFolder))
            {
                Directory.Delete(_cacheFolder, true);
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Windows keeps a library locked for as long as it is loaded. The next start clears what is left.
            _logger.Log($"Couldn't delete {_cacheFolder}: {exception.Message}", this, LogType.Warning);
        }
    }

    /// <summary>Copies the whole folder, because a native library can need the ones next to it. A plugin's folder is small.</summary>
    private string CopyToCache(string path)
    {
        string source = Path.GetDirectoryName(path)!;
        string folder = Path.Combine(_cacheFolder, Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(source)))[..16]);
        Directory.CreateDirectory(folder);

        foreach (string file in Directory.EnumerateFiles(source))
        {
            string destination = Path.Combine(folder, Path.GetFileName(file));
            if (!File.Exists(destination))
            {
                File.Copy(file, destination);
            }
        }

        return Path.Combine(folder, Path.GetFileName(path));
    }
}
