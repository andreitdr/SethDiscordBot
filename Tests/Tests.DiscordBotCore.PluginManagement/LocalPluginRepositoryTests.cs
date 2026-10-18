using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Microsoft.Data.Sqlite;

namespace Tests.DiscordBotCore.PluginManagement;

/// <summary>The installed-plugin database (<c>plugins.db</c>), on a real SQLite file.</summary>
public class LocalPluginRepositoryTests : IAsyncLifetime
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-plugin-db-tests", Guid.NewGuid().ToString("N"));
    private LocalPluginRepository _repository = null!;

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        _repository = new LocalPluginRepository(Path.Combine(_folder, "plugins.db"));
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _repository.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, true);
    }

    private static LocalPlugin Plugin(string name, string version = "1.0.0", bool enabled = true, bool offline = false) => new()
    {
        PluginName = name,
        PluginVersion = version,
        FilePath = $"./Data/Plugins/{name}.dll",
        IsEnabled = enabled,
        IsOfflineAdded = offline
    };

    private static LocalDependencyInfo Dependency(string name, Guid pluginId, string location = "bin/tool") => new()
    {
        DependencyName = name,
        DependencyLocation = location,
        PluginId = pluginId
    };

    [Fact]
    public async Task NewDatabase_IsEmpty()
    {
        Assert.Empty(await _repository.GetAllPluginsAsync());
        Assert.Null(await _repository.GetPluginByNameAsync("Missing"));
    }

    [Fact]
    public async Task AddPluginAsync_StoresEveryField()
    {
        Guid id = await _repository.AddPluginAsync(Plugin("Leveling", "1.2.3", enabled: false, offline: true));

        LocalPlugin? plugin = await _repository.GetPluginByNameAsync("Leveling");
        Assert.NotNull(plugin);
        Assert.Equal(id, plugin.Id);
        Assert.Equal("Leveling", plugin.PluginName);
        Assert.Equal("1.2.3", plugin.PluginVersion);
        Assert.Equal("./Data/Plugins/Leveling.dll", plugin.FilePath);
        Assert.False(plugin.IsEnabled);
        Assert.True(plugin.IsOfflineAdded);
    }

    [Fact]
    public async Task GetAllPluginsAsync_ReturnsEveryPlugin()
    {
        await _repository.AddPluginAsync(Plugin("A"));
        await _repository.AddPluginAsync(Plugin("B"));

        Assert.Equal(["A", "B"], (await _repository.GetAllPluginsAsync()).Select(p => p.PluginName).Order());
    }

    [Fact]
    public async Task AddPluginAsync_RejectsADuplicateName()
    {
        await _repository.AddPluginAsync(Plugin("A"));

        await Assert.ThrowsAsync<SqliteException>(() => _repository.AddPluginAsync(Plugin("A", "2.0.0")));
    }

    [Fact]
    public async Task GetPluginByNameAsync_TreatsTheNameAsAValue()
    {
        await _repository.AddPluginAsync(Plugin("A"));

        Assert.Null(await _repository.GetPluginByNameAsync("x' OR '1'='1"));
    }

    [Fact]
    public async Task UpdatePluginAsync_SavesTheEnabledState()
    {
        await _repository.AddPluginAsync(Plugin("A"));
        LocalPlugin plugin = (await _repository.GetPluginByNameAsync("A"))!;
        plugin.IsEnabled = false;

        Assert.True(await _repository.UpdatePluginAsync(plugin));

        Assert.False((await _repository.GetPluginByNameAsync("A"))!.IsEnabled);
    }

    [Fact]
    public async Task UpdatePluginAsync_SavesEveryFieldOfThatPluginOnly()
    {
        await _repository.AddPluginAsync(Plugin("A"));
        await _repository.AddPluginAsync(Plugin("B"));
        LocalPlugin plugin = (await _repository.GetPluginByNameAsync("A"))!;
        plugin.PluginVersion = "2.0.0";
        plugin.FilePath = "./other.dll";
        plugin.IsOfflineAdded = true;

        await _repository.UpdatePluginAsync(plugin);

        LocalPlugin updated = (await _repository.GetPluginByNameAsync("A"))!;
        Assert.Equal("2.0.0", updated.PluginVersion);
        Assert.Equal("./other.dll", updated.FilePath);
        Assert.True(updated.IsOfflineAdded);
        Assert.Equal("1.0.0", (await _repository.GetPluginByNameAsync("B"))!.PluginVersion);
    }

    [Fact]
    public async Task UpdatePluginAsync_ReturnsFalseForAnUnknownPlugin()
    {
        Assert.False(await _repository.UpdatePluginAsync(Plugin("Ghost")));
    }

    [Fact]
    public async Task DeletePluginAsync_RemovesOnlyThatPlugin()
    {
        Guid a = await _repository.AddPluginAsync(Plugin("A"));
        await _repository.AddPluginAsync(Plugin("B"));

        Assert.True(await _repository.DeletePluginAsync(a));

        Assert.Null(await _repository.GetPluginByNameAsync("A"));
        Assert.NotNull(await _repository.GetPluginByNameAsync("B"));
        Assert.False(await _repository.DeletePluginAsync(a));
    }

    [Fact]
    public async Task Dependencies_AreStoredAndReadPerPlugin()
    {
        Guid a = await _repository.AddPluginAsync(Plugin("A"));
        Guid b = await _repository.AddPluginAsync(Plugin("B"));
        Guid dependencyId = await _repository.AddDependencyAsync(Dependency("ffmpeg", a, "tools/ffmpeg"));
        await _repository.AddDependencyAsync(Dependency("other", b));

        LocalDependencyInfo dependency = Assert.Single(await _repository.GetDependenciesForPluginAsync("A"));
        Assert.Equal(dependencyId, dependency.Id);
        Assert.Equal("ffmpeg", dependency.DependencyName);
        Assert.Equal("tools/ffmpeg", dependency.DependencyLocation);
        Assert.Equal(a, dependency.PluginId);

        Assert.Equal("ffmpeg", Assert.Single(await _repository.GetDependenciesForPluginAsync(a)).DependencyName);
        Assert.Empty(await _repository.GetDependenciesForPluginAsync("Missing"));
    }

    [Fact]
    public async Task DeleteDependencyAsync_RemovesOneDependency()
    {
        Guid a = await _repository.AddPluginAsync(Plugin("A"));
        Guid first = await _repository.AddDependencyAsync(Dependency("first", a));
        await _repository.AddDependencyAsync(Dependency("second", a));

        Assert.True(await _repository.DeleteDependencyAsync(first));

        Assert.Equal("second", Assert.Single(await _repository.GetDependenciesForPluginAsync(a)).DependencyName);
    }

    [Fact]
    public async Task DeleteAllDependenciesForPluginAsync_KeepsOtherPluginsDependencies()
    {
        Guid a = await _repository.AddPluginAsync(Plugin("A"));
        Guid b = await _repository.AddPluginAsync(Plugin("B"));
        await _repository.AddDependencyAsync(Dependency("a1", a));
        await _repository.AddDependencyAsync(Dependency("a2", a));
        await _repository.AddDependencyAsync(Dependency("b1", b));

        Assert.True(await _repository.DeleteAllDependenciesForPluginAsync(a));

        Assert.Empty(await _repository.GetDependenciesForPluginAsync(a));
        Assert.Single(await _repository.GetDependenciesForPluginAsync(b));
    }

    [Fact]
    public async Task Data_SurvivesReopeningTheDatabase()
    {
        await _repository.AddPluginAsync(Plugin("A"));
        await _repository.DisposeAsync();

        _repository = new LocalPluginRepository(Path.Combine(_folder, "plugins.db"));

        Assert.NotNull(await _repository.GetPluginByNameAsync("A"));
    }
}
