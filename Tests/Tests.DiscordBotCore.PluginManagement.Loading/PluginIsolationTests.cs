using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using Discord.WebSocket;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Loading;
using DiscordBotCore.PluginManagement.Models;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement.Loading;

/// <summary>Skips where there's no C compiler to build the test's native library, such as on Windows.</summary>
public sealed class NativeCompilerFactAttribute : FactAttribute
{
    public NativeCompilerFactAttribute()
    {
        bool hasCompiler = !OperatingSystem.IsWindows() && (Environment.GetEnvironmentVariable("PATH") ?? "")
            .Split(Path.PathSeparator)
            .Any(folder => File.Exists(Path.Combine(folder, "cc")));

        if (!hasCompiler)
        {
            Skip = "Needs a C compiler";
        }
    }
}

/// <summary>Real plugins with real packages: each loads its own dependencies, and unloading removes them again.</summary>
public class PluginIsolationTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-isolation-tests", Guid.NewGuid().ToString("N"));
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly List<LocalPlugin> _installed = new();
    private readonly List<string> _librariesToDelete = new();
    private readonly List<string> _warnings = new();
    private readonly string _suffix = Guid.NewGuid().ToString("N");

    public PluginIsolationTests()
    {
        Directory.CreateDirectory(_folder);
        _pluginManager.Setup(m => m.GetInstalledPlugins()).ReturnsAsync(() => _installed.ToList());
        _configuration.Setup(c => c.Get("FreeNativeLibraries", true)).Returns(true);
    }

    public void Dispose()
    {
        foreach (string library in _librariesToDelete.Where(Directory.Exists))
        {
            Directory.Delete(library, true);
        }

        Directory.Delete(_folder, true);
    }

    private static string FixtureOutput(string fixture)
    {
        // <repo>/Tests/<this project>/bin/<configuration>/net10.0/
        string baseDirectory = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
        string framework = Path.GetFileName(baseDirectory);
        string configuration = Path.GetFileName(Path.GetDirectoryName(baseDirectory)!);
        string tests = Path.GetFullPath(Path.Combine(baseDirectory, "..", "..", "..", ".."));
        return Path.Combine(tests, "TestPlugins", fixture, "bin", configuration, framework);
    }

    private PluginLoader CreateLoader()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.Log(It.IsAny<string>(), It.IsAny<object>(), LogType.Warning))
              .Callback<string, object, LogType>((message, _, _) => _warnings.Add(message));
        return new PluginLoader(_pluginManager.Object, logger.Object, _configuration.Object);
    }

    /// <summary>Installs a fixture the way a marketplace install lays it out, and returns its name.</summary>
    private string Install(string fixture, params string[] libraries)
    {
        string name = $"{fixture}_{_suffix}";
        string output = FixtureOutput(fixture);
        string target = Path.Combine(AppContext.BaseDirectory, HostRuntime.LibrariesFolderName, name);
        _librariesToDelete.Add(target);

        foreach (string library in libraries)
        {
            string destination = Path.Combine(target, library);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(Path.Combine(output, library), destination);
        }

        string pluginFile = Path.Combine(_folder, name + ".dll");
        File.Copy(Path.Combine(output, fixture + ".dll"), pluginFile);
        _installed.Add(new LocalPlugin { Id = Guid.NewGuid(), PluginName = name, PluginVersion = "1.0.0", FilePath = pluginFile, IsEnabled = true });
        return name;
    }

    private static object? Call(PluginLoader loader, string plugin, string type, string method)
    {
        Assembly assembly = loader.LoadedPlugins.Single(p => p.Plugin.PluginName == plugin).Assembly;
        try
        {
            return assembly.GetType(type)!.GetMethod(method)!.Invoke(null, null);
        }
        catch (TargetInvocationException exception)
        {
            throw exception.InnerException!;
        }
    }

    private static string NativeLibraryFile()
    {
        string file = OperatingSystem.IsWindows() ? "libsodium.dll" : OperatingSystem.IsMacOS() ? "libsodium.dylib" : "libsodium.so";
        string root = Path.Combine(FixtureOutput("Tests.NativePlugin"), "runtimes");
        string? rid = HostRuntime.RuntimeIdentifiers.FirstOrDefault(r => File.Exists(Path.Combine(root, r, "native", file)));
        return Path.Combine("runtimes", rid ?? throw new InvalidOperationException("libsodium has no build for this platform"), "native", file);
    }

    /// <summary>
    /// How many copies of a library are mapped into the process, which is what "unloaded" really means. Each load
    /// uses its own copy, and other tests in this process may have left theirs, so tests compare before and after.
    /// </summary>
    private static int CountMappedCopies(string fileName)
    {
        if (OperatingSystem.IsWindows())
        {
            return Process.GetCurrentProcess().Modules.Cast<ProcessModule>().Count(module => module.FileName.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));
        }

        string map;
        if (OperatingSystem.IsLinux())
        {
            map = File.ReadAllText("/proc/self/maps");
        }
        else
        {
            using var process = Process.Start(new ProcessStartInfo("/usr/bin/vmmap", $"-w {Environment.ProcessId}") { RedirectStandardOutput = true, RedirectStandardError = true })!;
            process.ErrorDataReceived += (_, _) => { };
            process.BeginErrorReadLine();
            map = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
        }

        return Regex.Matches(map, @"/\S*" + Regex.Escape(fileName)).Select(match => match.Value).Distinct().Count();
    }

    [Fact]
    public async Task TwoPluginsUseDifferentVersionsOfTheSamePackage()
    {
        string a = Install("Tests.VersionPluginA", "Humanizer.dll");
        string b = Install("Tests.VersionPluginB", "Humanizer.dll");
        PluginLoader loader = CreateLoader();
        await loader.LoadPlugins();

        string first = (string)Call(loader, a, "Tests.VersionPluginA.VersionReader", "Read")!;
        string second = (string)Call(loader, b, "Tests.VersionPluginB.VersionReader", "Read")!;

        Assert.StartsWith("2.14.", first);
        Assert.StartsWith("3.0.", second);
        Assert.EndsWith("ome text", first);
        Assert.EndsWith("ome text", second);

        await loader.UnloadAllPlugins();
        Assert.Empty(loader.PluginsStuckAfterUnload);
    }

    [Fact]
    public async Task ThePackageIsNotLoadedIntoTheBotItself()
    {
        string a = Install("Tests.VersionPluginA", "Humanizer.dll");
        PluginLoader loader = CreateLoader();
        await loader.LoadPlugins();

        Call(loader, a, "Tests.VersionPluginA.VersionReader", "Read");

        Assert.DoesNotContain(AssemblyLoadContext.Default.Assemblies, assembly => assembly.GetName().Name == "Humanizer");
        await loader.UnloadAllPlugins();
    }

    [Fact]
    public async Task APluginWithoutItsPackageFailsAlone()
    {
        string withoutPackage = Install("Tests.VersionPluginA");
        string complete = Install("Tests.VersionPluginB", "Humanizer.dll");
        PluginLoader loader = CreateLoader();
        await loader.LoadPlugins();

        await Assert.ThrowsAnyAsync<Exception>(() => Task.FromResult(Call(loader, withoutPackage, "Tests.VersionPluginA.VersionReader", "Read")));
        Assert.NotNull(Call(loader, complete, "Tests.VersionPluginB.VersionReader", "Read"));
        await loader.UnloadAllPlugins();
    }

    [NativeCompilerFact]
    public async Task UnloadingRemovesTheNativeLibraryFromTheProcess()
    {
        string plugin = Install("Tests.NativePlugin");
        string nativeName = CompileAnswerLibrary(plugin, 42);
        PluginLoader loader = CreateLoader();
        int before = CountMappedCopies(nativeName);

        Assert.Equal(42, await LoadAndCallAnswerAsync(loader, plugin));
        Assert.Equal(before + 1, CountMappedCopies(nativeName));

        await loader.UnloadAllPlugins();

        Assert.Empty(loader.PluginsStuckAfterUnload);
        Assert.Equal(before, CountMappedCopies(nativeName));
    }

    [NativeCompilerFact]
    public async Task AReloadPicksUpAReplacedNativeLibrary_WithoutARestart()
    {
        string plugin = Install("Tests.NativePlugin");
        string nativeName = CompileAnswerLibrary(plugin, 1);
        PluginLoader loader = CreateLoader();
        int before = CountMappedCopies(nativeName);

        Assert.Equal(1, await LoadAndCallAnswerAsync(loader, plugin));
        await loader.UnloadAllPlugins();

        CompileAnswerLibrary(plugin, 2);

        Assert.Equal(2, await LoadAndCallAnswerAsync(loader, plugin));
        await loader.UnloadAllPlugins();
        Assert.Equal(before, CountMappedCopies(nativeName));
    }

    /// <summary>
    /// libsodium has thread-local variables, and macOS never unmaps a library that does. Each load has its own copy, so
    /// loading again still works.
    /// </summary>
    [Fact]
    public async Task ALibraryTheOsWontUnmap_DoesNotStopAReload()
    {
        string file = NativeLibraryFile();
        string plugin = Install("Tests.NativePlugin", file);
        PluginLoader loader = CreateLoader();

        await LoadAndCallNativeAsync(loader, plugin);
        await loader.UnloadAllPlugins();
        Assert.Empty(loader.PluginsStuckAfterUnload);

        string original = Path.Combine(AppContext.BaseDirectory, HostRuntime.LibrariesFolderName, plugin, file);
        File.Copy(Path.Combine(FixtureOutput("Tests.NativePlugin"), file), original, overwrite: true);

        await LoadAndCallNativeAsync(loader, plugin);
        await loader.UnloadAllPlugins();
        Assert.Empty(loader.PluginsStuckAfterUnload);
    }

    /// <summary>Compiles libanswer into the plugin's folder, the way a package's native library is installed there.</summary>
    private string CompileAnswerLibrary(string plugin, int answer)
    {
        string target = Path.Combine(AppContext.BaseDirectory, HostRuntime.LibrariesFolderName, plugin);
        Directory.CreateDirectory(target);
        string fileName = OperatingSystem.IsMacOS() ? "libanswer.dylib" : "libanswer.so";
        string source = Path.Combine(_folder, $"answer{answer}.c");
        File.WriteAllText(source, $"int answer(void) {{ return {answer}; }}\n");

        using var compiler = Process.Start(new ProcessStartInfo("cc", ["-shared", "-fPIC", "-o", Path.Combine(target, fileName), source]) { RedirectStandardError = true })!;
        string errors = compiler.StandardError.ReadToEnd();
        compiler.WaitForExit();
        Assert.True(compiler.ExitCode == 0, errors);
        return fileName;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<int> LoadAndCallAnswerAsync(PluginLoader loader, string plugin)
    {
        await loader.LoadPlugins();
        return (int)Call(loader, plugin, "Tests.NativePlugin.NativeCalls", "Answer")!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task LoadAndCallNativeAsync(PluginLoader loader, string plugin)
    {
        await loader.LoadPlugins();
        object? version = Call(loader, plugin, "Tests.NativePlugin.NativeCalls", "Read");
        Assert.IsType<int>(version);
    }

    [NativeCompilerFact]
    public async Task APluginThatLeavesSomethingRunning_IsReported_AndItsNativeLibraryIsKept()
    {
        string plugin = Install("Tests.NativePlugin");
        string nativeName = CompileAnswerLibrary(plugin, 7);
        PluginLoader loader = CreateLoader();
        int before = CountMappedCopies(nativeName);

        Assert.Equal(7, await LoadAndCallAnswerAsync(loader, plugin));
        Call(loader, plugin, "Tests.NativePlugin.NativeCalls", "LeaveTimerRunning");

        await loader.UnloadAllPlugins();

        Assert.Equal([plugin], loader.PluginsStuckAfterUnload);
        Assert.Contains(_warnings, message => message.Contains(plugin) && message.Contains("Restart"));

        // Freeing it now could crash the plugin's running code, so it must still be there
        Assert.Equal(before + 1, CountMappedCopies(nativeName));

        // And the loader still works: the plugin loads alongside the stuck copy
        Assert.Equal(7, await LoadAndCallAnswerAsync(loader, plugin));
        Assert.True(loader.IsLoaded);
    }

    [NativeCompilerFact]
    public async Task WithFreeingSwitchedOff_TheLibraryStaysLoaded()
    {
        _configuration.Setup(c => c.Get("FreeNativeLibraries", true)).Returns(false);
        string plugin = Install("Tests.NativePlugin");
        string nativeName = CompileAnswerLibrary(plugin, 3);
        PluginLoader loader = CreateLoader();
        int before = CountMappedCopies(nativeName);

        await LoadAndCallAnswerAsync(loader, plugin);
        await loader.UnloadAllPlugins();

        Assert.Empty(loader.PluginsStuckAfterUnload);
        Assert.Equal(before + 1, CountMappedCopies(nativeName));
    }
}
