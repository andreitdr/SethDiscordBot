using System.IO.Compression;
using System.Reflection;
using System.Text;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Moq;
using NuGet.Versioning;

namespace Tests.DiscordBotCore.PluginManagement;

/// <summary>The installer against packages built here. No network involved.</summary>
public class NuGetDependencyInstallerTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-nuget-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeSource _source = new();

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    private sealed class FakeSource : INuGetPackageSource
    {
        public Dictionary<string, byte[]> Packages { get; } = new(StringComparer.OrdinalIgnoreCase);
        public List<string> Downloads { get; } = new();

        public Task<byte[]?> DownloadAsync(string packageId, NuGetVersion version, CancellationToken cancellationToken)
        {
            Downloads.Add($"{packageId} {version}");
            return Task.FromResult(Packages.GetValueOrDefault($"{packageId} {version}"));
        }

        public Task<IReadOnlyList<NuGetVersion>> GetVersionsAsync(string packageId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<NuGetVersion>>(Packages.Keys
                .Where(key => key.StartsWith(packageId + " ", StringComparison.OrdinalIgnoreCase))
                .Select(key => NuGetVersion.Parse(key.Split(' ')[1]))
                .ToList());
    }

    private void Package(string id, string version, IEnumerable<string> files, params (string Id, string Range)[] dependencies)
    {
        string dependencyXml = string.Join("", dependencies.Select(d => $"""<dependency id="{d.Id}" version="{d.Range}" />"""));
        string nuspec = $"""
            <?xml version="1.0" encoding="utf-8"?>
            <package xmlns="http://schemas.microsoft.com/packaging/2013/05/nuspec.xsd">
              <metadata>
                <id>{id}</id>
                <version>{version}</version>
                <authors>test</authors>
                <description>test</description>
                <dependencies><group targetFramework="net8.0">{dependencyXml}</group></dependencies>
              </metadata>
            </package>
            """;

        using var buffer = new MemoryStream();
        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            WriteEntry(archive, $"{id}.nuspec", Encoding.UTF8.GetBytes(nuspec));
            foreach (string file in files)
            {
                WriteEntry(archive, file, Encoding.UTF8.GetBytes(file));
            }
        }

        _source.Packages[$"{id} {version}"] = buffer.ToArray();
    }

    private static void WriteEntry(ZipArchive archive, string path, byte[] content)
    {
        using Stream entry = archive.CreateEntry(path).Open();
        entry.Write(content);
    }

    private Task InstallAsync(params (string Id, string Version)[] packages) =>
        new NuGetDependencyInstaller(_source, new Mock<ILogger>().Object)
            .InstallAsync(packages.Select(p => new OnlineNuGetDependency(p.Id, p.Version)).ToList(), _folder);

    private bool Installed(string relativePath) => File.Exists(Path.Combine(_folder, relativePath));

    [Fact]
    public async Task InstallsThePackageAndWhatItDependsOn()
    {
        Package("Top", "1.0.0", ["lib/net8.0/Top.dll"], ("Middle", "2.0.0"));
        Package("Middle", "2.0.0", ["lib/net8.0/Middle.dll"], ("Bottom", "[3.0.0, )"));
        Package("Bottom", "3.0.0", ["lib/net8.0/Bottom.dll"]);

        await InstallAsync(("Top", "1.0.0"));

        Assert.True(Installed("Top.dll"));
        Assert.True(Installed("Middle.dll"));
        Assert.True(Installed("Bottom.dll"));
    }

    [Fact]
    public async Task WhenTwoVersionsAreAskedFor_TheHighestWins()
    {
        Package("A", "1.0.0", ["lib/net8.0/A.dll"], ("Shared", "1.0.0"));
        Package("B", "1.0.0", ["lib/net8.0/B.dll"], ("Shared", "2.0.0"));
        Package("Shared", "1.0.0", ["lib/net8.0/Shared.dll", "lib/net8.0/OnlyInOld.dll"]);
        Package("Shared", "2.0.0", ["lib/net8.0/Shared.dll"]);

        await InstallAsync(("A", "1.0.0"), ("B", "1.0.0"));

        Assert.True(Installed("Shared.dll"));
        Assert.False(Installed("OnlyInOld.dll"));
    }

    [Fact]
    public async Task PicksTheNearestFramework_AndOnlyDlls()
    {
        Package("Lib", "1.0.0", ["lib/netstandard2.0/Old.dll", "lib/net8.0/New.dll", "lib/net8.0/New.xml", "lib/net48/Framework.dll"]);

        await InstallAsync(("Lib", "1.0.0"));

        Assert.True(Installed("New.dll"));
        Assert.False(Installed("New.xml"));
        Assert.False(Installed("Old.dll"));
        Assert.False(Installed("Framework.dll"));
    }

    [Fact]
    public async Task RuntimeSpecificLibrariesReplaceTheGeneralOnes()
    {
        string rid = HostRuntime.RuntimeIdentifiers[0];
        Package("Lib", "1.0.0", ["lib/net8.0/General.dll", $"runtimes/{rid}/lib/net8.0/Specific.dll"]);

        await InstallAsync(("Lib", "1.0.0"));

        Assert.True(Installed("Specific.dll"));
        Assert.False(Installed("General.dll"));
    }

    [Fact]
    public async Task NativeLibrariesOfThisPlatformAreInstalled_OthersArent()
    {
        string rid = HostRuntime.RuntimeIdentifiers[0];
        Package("Lib", "1.0.0", [$"runtimes/{rid}/native/libfoo.so", "runtimes/other-rid/native/libbar.so"]);

        await InstallAsync(("Lib", "1.0.0"));

        Assert.True(Installed($"runtimes/{rid}/native/libfoo.so"));
        Assert.False(Installed("runtimes/other-rid/native/libbar.so"));
    }

    [Fact]
    public async Task SatelliteAssembliesKeepTheirCultureFolder()
    {
        Package("Lib", "1.0.0", ["lib/net8.0/Lib.dll", "lib/net8.0/fr/Lib.resources.dll"]);

        await InstallAsync(("Lib", "1.0.0"));

        Assert.True(Installed("fr/Lib.resources.dll"));
    }

    [Fact]
    public async Task DependenciesTheBotAlreadyShipsAreNotDownloaded()
    {
        string shipped = typeof(object).Assembly.GetName().Name!;
        Package("Lib", "1.0.0", ["lib/net8.0/Lib.dll"], (shipped, "1.0.0"));

        await InstallAsync(("Lib", "1.0.0"));

        Assert.Equal(["Lib 1.0.0"], _source.Downloads);
    }

    [Fact]
    public async Task AnAssemblyTheBotShipsIsntCopied()
    {
        AssemblyName shipped = typeof(Moq.Mock).Assembly.GetName();
        Package("Lib", "1.0.0", [$"lib/net8.0/{shipped.Name}.dll"]);

        await InstallAsync(("Lib", "1.0.0"));

        Assert.False(Installed($"{shipped.Name}.dll"));
    }

    [Fact]
    public async Task AMissingPackageFailsWithAClearMessage()
    {
        var exception = await Assert.ThrowsAsync<NuGetInstallException>(() => InstallAsync(("Missing", "1.0.0")));

        Assert.Contains("Missing 1.0.0", exception.Message);
    }

    [Fact]
    public async Task AnInvalidVersionFailsWithAClearMessage()
    {
        var exception = await Assert.ThrowsAsync<NuGetInstallException>(() => InstallAsync(("Lib", "not-a-version")));

        Assert.Contains("invalid version", exception.Message);
    }

    [Fact]
    public async Task ACorruptPackageFailsWithAClearMessage()
    {
        _source.Packages["Lib 1.0.0"] = [1, 2, 3];

        await Assert.ThrowsAsync<NuGetInstallException>(() => InstallAsync(("Lib", "1.0.0")));
    }
}
