using Discord.WebSocket;
using DiscordBotCore.Bot;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Loading;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities.Responses;
using Moq;
using WebUI.Services;

namespace Tests.WebUI;

/// <summary>The dashboard's plugin actions: add a local DLL, enable, uninstall, install from the marketplace.</summary>
public class PluginCatalogServiceTests : IDisposable
{
    private readonly Mock<IPluginManager> _manager = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly Mock<IPluginLoader> _loader = new();
    private readonly string _pluginFolder = Path.Combine(Path.GetTempPath(), "seth-catalog-tests", Guid.NewGuid().ToString("N"));
    private readonly List<LocalPlugin> _installed = new();

    public PluginCatalogServiceTests()
    {
        _configuration.Setup(c => c.Get("PluginFolder", It.IsAny<string>())).Returns(_pluginFolder);
        _manager.Setup(m => m.GetInstalledPlugins()).ReturnsAsync(() => _installed.ToList());
        _manager.Setup(m => m.IsRepositoryConfigured).Returns(true);
        _manager.Setup(m => m.CreateOfflineLocalPlugin(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync((string name, string path, string version, bool enabled) =>
                    new LocalPlugin { PluginName = name, FilePath = path, PluginVersion = version, IsEnabled = enabled, IsOfflineAdded = true });
        _manager.Setup(m => m.AppendPluginToDatabase(It.IsAny<LocalPlugin>(), It.IsAny<List<LocalDependencyInfo>>()))
                .Callback<LocalPlugin, List<LocalDependencyInfo>>((plugin, _) => _installed.Add(plugin))
                .ReturnsAsync(Response.Success());
    }

    public void Dispose()
    {
        if (Directory.Exists(_pluginFolder))
        {
            Directory.Delete(_pluginFolder, true);
        }
    }

    private PluginCatalogService Create()
    {
        var bot = new Mock<IDiscordBotApplication>();
        var state = new BotStateService(bot.Object, _loader.Object, _configuration.Object, new Mock<ILogger>().Object);
        return new PluginCatalogService(_manager.Object, state, _configuration.Object, new Mock<ILogger>().Object);
    }

    private static Stream Content(string text = "MZ fake dll") => new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));

    private static OnlinePlugin Online(int id = 7) => new(id, "Leveling", "", "1.0.0", "author", "https://x/p.dll", 1, true);

    #region Add local

    [Fact]
    public async Task AddLocal_SavesTheFileAndAddsThePlugin()
    {
        var catalog = Create();

        IResponse<bool> response = await catalog.AddLocalAsync(" Leveling ", "1.2.3", false, "LevelingSystem.dll", Content("bytes"));

        Assert.True(response.IsSuccess, response.Message);
        string file = Path.Combine(_pluginFolder, "LevelingSystem.dll");
        Assert.Equal("bytes", await File.ReadAllTextAsync(file));
        _manager.Verify(m => m.CreateOfflineLocalPlugin("Leveling", file, "1.2.3", false), Times.Once);
        Assert.True(catalog.IsInstalled("leveling"));
    }

    [Fact]
    public async Task AddLocal_KeepsOnlyTheFileNameFromTheBrowser()
    {
        var catalog = Create();

        IResponse<bool> response = await catalog.AddLocalAsync("Evil", "1.0.0", true, "../../outside/Evil.dll", Content());

        Assert.True(response.IsSuccess, response.Message);
        Assert.True(File.Exists(Path.Combine(_pluginFolder, "Evil.dll")));
        Assert.False(Directory.Exists(Path.Combine(_pluginFolder, "..", "..", "outside")));
    }

    [Theory]
    [InlineData("", "1.0.0", "a.dll", "needs a name")]
    [InlineData("   ", "1.0.0", "a.dll", "needs a name")]
    [InlineData("A", "1.0", "a.dll", "1.0.0")]
    [InlineData("A", "v1.0.0", "a.dll", "1.0.0")]
    [InlineData("A", "1.0.0", "a.exe", "Only .dll")]
    [InlineData("A", "1.0.0", "a.dll.txt", "Only .dll")]
    public async Task AddLocal_RejectsInvalidInput(string name, string version, string fileName, string error)
    {
        IResponse<bool> response = await Create().AddLocalAsync(name, version, true, fileName, Content());

        Assert.False(response.IsSuccess);
        Assert.Contains(error, response.Message);
        Assert.False(Directory.Exists(_pluginFolder) && Directory.EnumerateFiles(_pluginFolder).Any());
    }

    [Fact]
    public async Task AddLocal_RejectsANameThatIsAlreadyInstalled()
    {
        _installed.Add(new LocalPlugin { PluginName = "Leveling", PluginVersion = "1.0.0", FilePath = "x.dll" });
        var catalog = Create();
        await catalog.RefreshInstalledAsync();

        IResponse<bool> response = await catalog.AddLocalAsync("LEVELING", "2.0.0", true, "b.dll", Content());

        Assert.False(response.IsSuccess);
        Assert.Contains("already installed", response.Message);
    }

    [Fact]
    public async Task AddLocal_ReportsADatabaseFailure()
    {
        _manager.Setup(m => m.AppendPluginToDatabase(It.IsAny<LocalPlugin>(), It.IsAny<List<LocalDependencyInfo>>()))
                .ReturnsAsync(Response.Failure("database is locked"));

        IResponse<bool> response = await Create().AddLocalAsync("A", "1.0.0", true, "a.dll", Content());

        Assert.False(response.IsSuccess);
        Assert.Equal("database is locked", response.Message);
    }

    [Theory]
    [InlineData("1.0.0", true)]
    [InlineData("10.20.30", true)]
    [InlineData("1.0", false)]
    [InlineData("1.0.0-beta", false)]
    [InlineData(null, false)]
    public void IsValidVersion_AcceptsMajorMinorPatch(string? version, bool expected)
    {
        Assert.Equal(expected, PluginCatalogService.IsValidVersion(version));
    }

    #endregion

    #region Installed plugins

    [Fact]
    public async Task EnsureInstalledLoaded_ReadsTheListOnce()
    {
        var catalog = Create();

        await catalog.EnsureInstalledLoadedAsync();
        await catalog.EnsureInstalledLoadedAsync();

        Assert.True(catalog.InstalledLoaded);
        _manager.Verify(m => m.GetInstalledPlugins(), Times.Once);
    }

    [Fact]
    public async Task RefreshInstalled_SurvivesADatabaseError()
    {
        _manager.Setup(m => m.GetInstalledPlugins()).ThrowsAsync(new InvalidOperationException("corrupt"));
        var catalog = Create();

        await catalog.RefreshInstalledAsync();

        Assert.True(catalog.InstalledLoaded);
        Assert.Empty(catalog.Installed);
    }

    [Fact]
    public async Task SetEnabled_SavesAndRefreshes()
    {
        var catalog = Create();

        IResponse<bool> response = await catalog.SetEnabledAsync("A", false);

        Assert.True(response.IsSuccess);
        _manager.Verify(m => m.SetEnabledStatus("A", false), Times.Once);
        Assert.True(catalog.InstalledLoaded);
    }

    [Fact]
    public async Task SetEnabled_ReportsAFailure()
    {
        _manager.Setup(m => m.SetEnabledStatus("A", true)).ThrowsAsync(new InvalidOperationException("Must add values"));

        IResponse<bool> response = await Create().SetEnabledAsync("A", true);

        Assert.False(response.IsSuccess);
        Assert.Contains("Must add values", response.Message);
    }

    [Fact]
    public async Task Uninstall_DeletesThroughThePluginManager()
    {
        _manager.Setup(m => m.UninstallPluginByName("A")).ReturnsAsync(Response.Success());

        IResponse<bool> response = await Create().UninstallAsync("A");

        Assert.True(response.IsSuccess);
        _manager.Verify(m => m.UninstallPluginByName("A"), Times.Once);
    }

    [Fact]
    public async Task Uninstall_ReportsAFailure()
    {
        _manager.Setup(m => m.UninstallPluginByName("A")).ReturnsAsync(Response.Failure("not found"));

        Assert.Equal("not found", (await Create().UninstallAsync("A")).Message);
    }

    #endregion

    #region Marketplace

    [Fact]
    public async Task RefreshOnline_ListsTheRepositoryPlugins()
    {
        _manager.Setup(m => m.GetOnlinePluginsList()).ReturnsAsync([Online()]);
        var catalog = Create();

        await catalog.EnsureOnlineLoadedAsync();

        Assert.True(catalog.OnlineLoaded);
        Assert.False(catalog.OnlineLoading);
        Assert.Null(catalog.OnlineError);
        Assert.Single(catalog.Online);
    }

    [Fact]
    public async Task RefreshOnline_WithoutARepository_RequestsNothing()
    {
        _manager.Setup(m => m.IsRepositoryConfigured).Returns(false);
        var catalog = Create();

        await catalog.RefreshOnlineAsync();

        Assert.False(catalog.RepositoryConfigured);
        Assert.True(catalog.OnlineLoaded);
        Assert.Null(catalog.OnlineError);
        _manager.Verify(m => m.GetOnlinePluginsList(), Times.Never);
    }

    [Fact]
    public async Task RefreshOnline_ShowsAnErrorWhenTheRepositoryFails()
    {
        _manager.Setup(m => m.GetOnlinePluginsList()).ThrowsAsync(new HttpRequestException("Connection refused"));
        var catalog = Create();

        await catalog.RefreshOnlineAsync();

        Assert.Contains("Connection refused", catalog.OnlineError);
        Assert.False(catalog.OnlineLoading);
    }

    [Fact]
    public async Task Install_InstallsTheRepositoryVersion()
    {
        _manager.Setup(m => m.GetOnlinePluginDataById(7)).ReturnsAsync(Response<OnlinePlugin>.Success(Online()));
        _manager.Setup(m => m.InstallPlugin(It.IsAny<OnlinePlugin>(), It.IsAny<IProgress<float>>())).ReturnsAsync(Response.Success());
        var catalog = Create();

        IResponse<bool> response = await catalog.InstallAsync(Online());

        Assert.True(response.IsSuccess);
        Assert.False(catalog.TryGetInstallProgress(7, out _));
        Assert.True(catalog.InstalledLoaded);
    }

    [Fact]
    public async Task Install_ShowsProgressWhileDownloading()
    {
        var release = new TaskCompletionSource<IResponse<bool>>();
        _manager.Setup(m => m.GetOnlinePluginDataById(7)).ReturnsAsync(Response<OnlinePlugin>.Success(Online()));
        _manager.Setup(m => m.InstallPlugin(It.IsAny<OnlinePlugin>(), It.IsAny<IProgress<float>>())).Returns(release.Task);
        var catalog = Create();

        Task<IResponse<bool>> install = catalog.InstallAsync(Online());

        Assert.True(catalog.TryGetInstallProgress(7, out float percent));
        Assert.Equal(0f, percent);
        IResponse<bool> second = await catalog.InstallAsync(Online());
        Assert.False(second.IsSuccess);
        Assert.Contains("already being installed", second.Message);

        release.SetResult(Response.Success());
        await install;
        Assert.False(catalog.TryGetInstallProgress(7, out _));
    }

    [Fact]
    public async Task Install_ReportsWhenTheRepositoryDoesNotKnowThePlugin()
    {
        _manager.Setup(m => m.GetOnlinePluginDataById(7)).ReturnsAsync(Response<OnlinePlugin>.Failure("Plugin 7 not found"));

        IResponse<bool> response = await Create().InstallAsync(Online());

        Assert.Equal("Plugin 7 not found", response.Message);
        _manager.Verify(m => m.InstallPlugin(It.IsAny<OnlinePlugin>(), It.IsAny<IProgress<float>>()), Times.Never);
    }

    [Fact]
    public async Task Install_ReportsAnInstallFailure()
    {
        _manager.Setup(m => m.GetOnlinePluginDataById(7)).ReturnsAsync(Response<OnlinePlugin>.Success(Online()));
        _manager.Setup(m => m.InstallPlugin(It.IsAny<OnlinePlugin>(), It.IsAny<IProgress<float>>())).ReturnsAsync(Response.Failure("Download failed"));

        Assert.Equal("Download failed", (await Create().InstallAsync(Online())).Message);
    }

    [Fact]
    public async Task GetOnlineDetails_SurvivesAnUnreachableRepository()
    {
        _manager.Setup(m => m.GetOnlinePluginDataByName("A")).ThrowsAsync(new HttpRequestException("timeout"));

        IResponse<OnlinePlugin> response = await Create().GetOnlineDetailsAsync("A");

        Assert.False(response.IsSuccess);
        Assert.Contains("timeout", response.Message);
    }

    #endregion
}
