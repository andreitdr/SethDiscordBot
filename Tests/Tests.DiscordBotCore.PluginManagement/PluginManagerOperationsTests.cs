using System.Net;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement;

/// <summary>Installing, enabling, adding and looking up plugins, with a mocked database and a fake download server.</summary>
public class PluginManagerOperationsTests : IDisposable
{
    private readonly Mock<IPluginRepository> _remote = new();
    private readonly Mock<ILocalPluginRepository> _local = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly FakeHttpHandler _http = new();
    private readonly string _pluginFolder = Path.Combine(Path.GetTempPath(), "seth-install-tests", Guid.NewGuid().ToString("N"));
    private readonly string _pluginName = $"InstallTest_{Guid.NewGuid():N}";
    private readonly List<LocalPlugin> _added = new();
    private readonly List<LocalDependencyInfo> _addedDependencies = new();

    public PluginManagerOperationsTests()
    {
        Directory.CreateDirectory(_pluginFolder);
        _remote.Setup(r => r.IsConfigured).Returns(true);
        _remote.Setup(r => r.GetDependenciesForPlugin(It.IsAny<int>())).ReturnsAsync([]);
        _configuration.Setup(c => c.Get<string>("PluginFolder")).Returns(_pluginFolder);
        _local.Setup(r => r.AddPluginAsync(It.IsAny<LocalPlugin>())).Callback<LocalPlugin>(_added.Add).ReturnsAsync(Guid.NewGuid());
        _local.Setup(r => r.AddDependencyAsync(It.IsAny<LocalDependencyInfo>())).Callback<LocalDependencyInfo>(_addedDependencies.Add).ReturnsAsync(Guid.NewGuid());
        _local.Setup(r => r.GetAllPluginsAsync()).ReturnsAsync(() => _added.ToList());
    }

    public void Dispose()
    {
        Directory.Delete(_pluginFolder, true);
        string libraries = Path.Combine("Libraries", _pluginName);
        if (Directory.Exists(libraries))
        {
            Directory.Delete(libraries, true);
        }
    }

    private PluginManager CreateManager() =>
        new(_remote.Object, _local.Object, new Mock<ILogger>().Object, _configuration.Object, new HttpClient(_http));

    private OnlinePlugin OnlinePlugin(string version = "1.0.0", string? name = null) =>
        new(7, name ?? _pluginName, "description", version, "author", "https://files.example.com/plugin.dll", 1, true);

    private static OnlineDependencyInfo OnlineDependency(string name, string location, bool executable = false) =>
        new(name, $"https://files.example.com/{name}", location, executable);

    private static IProgress<float> NoProgress => new Progress<float>();

    #region InstallPlugin

    [Fact]
    public async Task InstallPlugin_DownloadsThePluginAndAddsItEnabled()
    {
        _http.Add("https://files.example.com/plugin.dll", [1, 2, 3]);

        var response = await CreateManager().InstallPlugin(OnlinePlugin(), NoProgress);

        Assert.True(response.IsSuccess, response.Message);
        string file = Path.Combine(_pluginFolder, $"{_pluginName}.dll");
        Assert.Equal([1, 2, 3], await File.ReadAllBytesAsync(file));
        LocalPlugin added = Assert.Single(_added);
        Assert.Equal(_pluginName, added.PluginName);
        Assert.Equal("1.0.0", added.PluginVersion);
        Assert.Equal($"{_pluginFolder}/{_pluginName}.dll", added.FilePath);
        Assert.True(added.IsEnabled);
        Assert.False(added.IsOfflineAdded);
    }

    [Fact]
    public async Task InstallPlugin_DownloadsDependenciesAndRecordsOnlyExecutables()
    {
        _http.Add("https://files.example.com/plugin.dll", [1])
             .Add("https://files.example.com/lib", [2])
             .Add("https://files.example.com/tool", [3]);
        _remote.Setup(r => r.GetDependenciesForPlugin(7)).ReturnsAsync([
            OnlineDependency("lib", "Lib.dll"),
            OnlineDependency("tool", "bin/tool", executable: true)
        ]);

        var response = await CreateManager().InstallPlugin(OnlinePlugin(), NoProgress);

        Assert.True(response.IsSuccess, response.Message);
        Assert.Equal([2], await File.ReadAllBytesAsync(Path.Combine("Libraries", _pluginName, "Lib.dll")));
        Assert.Equal([3], await File.ReadAllBytesAsync(Path.Combine("Libraries", _pluginName, "bin", "tool")));
        LocalDependencyInfo recorded = Assert.Single(_addedDependencies);
        Assert.Equal("tool", recorded.DependencyName);
        Assert.Equal("bin/tool", recorded.DependencyLocation);
    }

    [Fact]
    public async Task InstallPlugin_FailsCleanlyWhenTheDownloadFails()
    {
        var response = await CreateManager().InstallPlugin(OnlinePlugin(), NoProgress);

        Assert.False(response.IsSuccess);
        Assert.Contains("Download failed", response.Message);
        Assert.Empty(_added);
        Assert.False(File.Exists(Path.Combine(_pluginFolder, $"{_pluginName}.dll")));
    }

    [Theory]
    [InlineData("../escaped.dll")]
    [InlineData("../../escaped.dll")]
    [InlineData("/etc/escaped")]
    public async Task InstallPlugin_RejectsDependenciesOutsideThePluginFolder(string location)
    {
        _http.Add("https://files.example.com/plugin.dll", [1]).Add("https://files.example.com/evil", [9]);
        _remote.Setup(r => r.GetDependenciesForPlugin(7)).ReturnsAsync([OnlineDependency("evil", location)]);

        var response = await CreateManager().InstallPlugin(OnlinePlugin(), NoProgress);

        Assert.False(response.IsSuccess);
        Assert.Contains("outside", response.Message);
        Assert.Empty(_http.RequestedUrls);
        Assert.Empty(_added);
    }

    [Theory]
    [InlineData("../Escaped")]
    [InlineData("Group/Plugin")]
    [InlineData("..")]
    public async Task InstallPlugin_RejectsNamesThatAreNotFileNames(string name)
    {
        var response = await CreateManager().InstallPlugin(OnlinePlugin(name: name), NoProgress);

        Assert.False(response.IsSuccess);
        Assert.Empty(_http.RequestedUrls);
    }

    [Fact]
    public async Task InstallPlugin_UpgradesANewerVersionAndKeepsItDisabled()
    {
        var installed = new LocalPlugin { Id = Guid.NewGuid(), PluginName = _pluginName, PluginVersion = "1.0.0", FilePath = "old.dll", IsEnabled = false };
        _local.Setup(r => r.GetPluginByNameAsync(_pluginName)).ReturnsAsync(installed);
        _http.Add("https://files.example.com/plugin.dll", [1]);

        var response = await CreateManager().InstallPlugin(OnlinePlugin("1.1.0"), NoProgress);

        Assert.True(response.IsSuccess, response.Message);
        _local.Verify(r => r.DeletePluginAsync(installed.Id), Times.Once);
        LocalPlugin added = Assert.Single(_added);
        Assert.Equal("1.1.0", added.PluginVersion);
        Assert.False(added.IsEnabled);
    }

    [Theory]
    [InlineData("1.0.0", "Versions are the same")]
    [InlineData("0.9.0", "Current version is newer")]
    public async Task InstallPlugin_RefusesTheSameOrAnOlderVersion(string version, string message)
    {
        _local.Setup(r => r.GetPluginByNameAsync(_pluginName)).ReturnsAsync(
            new LocalPlugin { PluginName = _pluginName, PluginVersion = "1.0.0", FilePath = "x.dll" });

        var response = await CreateManager().InstallPlugin(OnlinePlugin(version), NoProgress);

        Assert.False(response.IsSuccess);
        Assert.Equal(message, response.Message);
        Assert.Empty(_http.RequestedUrls);
    }

    [Fact]
    public async Task InstallPlugin_NeedsThePluginFolderSetting()
    {
        _configuration.Setup(c => c.Get<string>("PluginFolder")).Returns((string?)null);

        var response = await CreateManager().InstallPlugin(OnlinePlugin(), NoProgress);

        Assert.False(response.IsSuccess);
        Assert.Contains("Plugin folder", response.Message);
    }

    [Fact]
    public async Task InstallPlugin_NeedsARepository()
    {
        _remote.Setup(r => r.IsConfigured).Returns(false);

        var response = await CreateManager().InstallPlugin(OnlinePlugin(), NoProgress);

        Assert.False(response.IsSuccess);
        Assert.Empty(_http.RequestedUrls);
    }

    #endregion

    #region Local plugins

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task SetEnabledStatus_SavesTheNewState(bool enabled)
    {
        var plugin = new LocalPlugin { PluginName = "A", PluginVersion = "1.0.0", FilePath = "a.dll", IsEnabled = !enabled };
        _local.Setup(r => r.GetPluginByNameAsync("A")).ReturnsAsync(plugin);

        await CreateManager().SetEnabledStatus("A", enabled);

        _local.Verify(r => r.UpdatePluginAsync(It.Is<LocalPlugin>(p => p.PluginName == "A" && p.IsEnabled == enabled)), Times.Once);
    }

    [Fact]
    public async Task SetEnabledStatus_IgnoresAnUnknownPlugin()
    {
        await CreateManager().SetEnabledStatus("Ghost", true);

        _local.Verify(r => r.UpdatePluginAsync(It.IsAny<LocalPlugin>()), Times.Never);
    }

    [Fact]
    public async Task CreateOfflineLocalPlugin_DescribesAnUploadedPlugin()
    {
        LocalPlugin plugin = await CreateManager().CreateOfflineLocalPlugin("A", "./Data/Plugins/a.dll", "2.0.0", false);

        Assert.NotEqual(Guid.Empty, plugin.Id);
        Assert.Equal("A", plugin.PluginName);
        Assert.Equal("2.0.0", plugin.PluginVersion);
        Assert.Equal("./Data/Plugins/a.dll", plugin.FilePath);
        Assert.False(plugin.IsEnabled);
        Assert.True(plugin.IsOfflineAdded);
    }

    [Fact]
    public async Task AppendPluginToDatabase_AddsThePluginAndItsDependencies()
    {
        var manager = CreateManager();
        LocalPlugin plugin = await manager.CreateOfflineLocalPlugin("A", "a.dll", "1.0.0", true);
        var dependency = new LocalDependencyInfo { DependencyName = "tool", DependencyLocation = "tool" };

        var response = await manager.AppendPluginToDatabase(plugin, [dependency]);

        Assert.True(response.IsSuccess);
        Assert.Same(plugin, Assert.Single(_added));
        Assert.NotEqual(Guid.Empty, Assert.Single(_addedDependencies).PluginId);
    }

    [Fact]
    public async Task AppendPluginToDatabase_SkipsTheSameNameAndVersion()
    {
        var manager = CreateManager();
        await manager.AppendPluginToDatabase(await manager.CreateOfflineLocalPlugin("A", "a.dll", "1.0.0", true), []);

        var response = await manager.AppendPluginToDatabase(await manager.CreateOfflineLocalPlugin("A", "a.dll", "1.0.0", true), []);

        Assert.True(response.IsSuccess);
        Assert.Single(_added);
    }

    [Fact]
    public async Task AppendPluginToDatabase_ReportsADatabaseError()
    {
        _local.Setup(r => r.AddPluginAsync(It.IsAny<LocalPlugin>())).ThrowsAsync(new InvalidOperationException("UNIQUE constraint failed"));
        var manager = CreateManager();

        var response = await manager.AppendPluginToDatabase(await manager.CreateOfflineLocalPlugin("A", "a.dll", "2.0.0", true), []);

        Assert.False(response.IsSuccess);
        Assert.Contains("UNIQUE", response.Message);
    }

    [Fact]
    public async Task GetDependencyLocation_ReturnsThePathUnderLibraries()
    {
        _local.Setup(r => r.GetDependenciesForPluginAsync("A")).ReturnsAsync([
            new LocalDependencyInfo { DependencyName = "ffmpeg", DependencyLocation = "bin/ffmpeg" }
        ]);

        var found = await CreateManager().GetDependencyLocation("ffmpeg", "A");
        var missing = await CreateManager().GetDependencyLocation("other", "A");

        Assert.Equal("./Libraries/A/bin/ffmpeg", found.Data);
        Assert.False(missing.IsSuccess);
    }

    [Fact]
    public async Task UninstallPluginByName_FailsForAnUnknownPlugin()
    {
        var response = await CreateManager().UninstallPluginByName("Ghost");

        Assert.False(response.IsSuccess);
    }

    #endregion

    #region Online lookups

    [Fact]
    public async Task GetOnlinePluginsList_AsksForThisOperatingSystem()
    {
        int os = global::DiscordBotCore.Utilities.OperatingSystem.GetOperatingSystemInt();
        _remote.Setup(r => r.GetAllPlugins(os, false)).ReturnsAsync([OnlinePlugin()]);

        Assert.Single(await CreateManager().GetOnlinePluginsList());
    }

    [Fact]
    public async Task GetOnlinePluginsList_IsEmptyWhenTheRepositoryHasNothing()
    {
        _remote.Setup(r => r.GetAllPlugins(It.IsAny<int>(), false)).ReturnsAsync([]);

        Assert.Empty(await CreateManager().GetOnlinePluginsList());
    }

    [Fact]
    public async Task GetOnlinePluginData_ReturnsThePluginOrAFailure()
    {
        _remote.Setup(r => r.GetPluginById(7)).ReturnsAsync(OnlinePlugin());
        _remote.Setup(r => r.GetPluginByName(_pluginName, It.IsAny<int>(), false)).ReturnsAsync(OnlinePlugin());
        var manager = CreateManager();

        Assert.True((await manager.GetOnlinePluginDataById(7)).IsSuccess);
        Assert.False((await manager.GetOnlinePluginDataById(8)).IsSuccess);
        Assert.True((await manager.GetOnlinePluginDataByName(_pluginName)).IsSuccess);
        Assert.False((await manager.GetOnlinePluginDataByName("Other")).IsSuccess);
    }

    #endregion
}
