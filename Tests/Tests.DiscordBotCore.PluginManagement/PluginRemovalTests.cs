using System.Net;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Microsoft.Data.Sqlite;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement;

/// <summary>
/// Plugins removed from the marketplace: the repository's <c>plugin/status</c> answer, how it's kept in the local
/// database, and what the bot does with it before loading plugins.
/// </summary>
public class PluginRemovalTests : IAsyncLifetime
{
    private const string Base = "https://plugins.example.com/api/v1/";

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-removal-tests", Guid.NewGuid().ToString("N"));
    private readonly FakeHttpHandler _http = new();
    private readonly Mock<IPluginRepository> _remote = new();
    private readonly Mock<ILogger> _logger = new();
    private LocalPluginRepository _local = null!;

    private string DatabaseFile => Path.Combine(_folder, "plugins.db");

    public Task InitializeAsync()
    {
        Directory.CreateDirectory(_folder);
        _local = new LocalPluginRepository(DatabaseFile);
        _remote.Setup(r => r.IsConfigured).Returns(true);
        return Task.CompletedTask;
    }

    public async Task DisposeAsync()
    {
        await _local.DisposeAsync();
        SqliteConnection.ClearAllPools();
        Directory.Delete(_folder, true);
    }

    private PluginManager CreateManager() =>
        new(_remote.Object, _local, _logger.Object, new Mock<IConfiguration>().Object, new HttpClient(_http));

    private PluginRepository CreateHttpRepository(int serverProtocol)
    {
        _http.Add(Base + "compatibility", Utf8($$"""{"protocolVersion":{{serverProtocol}},"minimumClientProtocolVersion":3}"""));
        return new PluginRepository(new PluginRepositoryConfiguration(Base, "plugin/", "dependency/"), _logger.Object, new HttpClient(_http));
    }

    private static byte[] Utf8(string text) => System.Text.Encoding.UTF8.GetBytes(text);

    private async Task<LocalPlugin> InstallAsync(string name, bool offline = false, bool enabled = true)
    {
        await _local.AddPluginAsync(new LocalPlugin
        {
            PluginName = name, PluginVersion = "1.0.0", FilePath = $"./Data/Plugins/{name}.dll", IsOfflineAdded = offline, IsEnabled = enabled
        });
        return (await _local.GetPluginByNameAsync(name))!;
    }

    private void RepositorySays(params OnlinePluginStatus[] statuses) =>
        _remote.Setup(r => r.GetPluginStatuses(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync(statuses.ToList());

    private static OnlinePluginStatus Removed(string name, PluginRemovalSeverity severity, bool byAdmin = false, string? reason = null) =>
        new(name, OnlinePluginAvailability.Removed,
            new PluginRemovalNotice(new DateTimeOffset(2026, 11, 9, 19, 51, 4, TimeSpan.Zero), byAdmin, severity, reason));

    #region The repository's answer

    [Fact]
    public async Task GetPluginStatuses_ParsesTheDocumentedAnswer()
    {
        _http.Add(Base + "plugin/status?names=Old%20One&names=Fine&names=Mine", Utf8("""
            [
              { "Name": "Old One", "Status": "Removed", "RemovedAt": "2026-11-09T19:51:04+00:00", "RemovedBy": "Administrator",
                "Severity": "Critical", "Reason": "Steals tokens – ☠" },
              { "Name": "Fine", "Status": "Available", "RemovedAt": null, "RemovedBy": null, "Severity": null, "Reason": null },
              { "Name": "Mine", "Status": "Unknown", "RemovedAt": null, "RemovedBy": null, "Severity": null, "Reason": null }
            ]
            """));

        List<OnlinePluginStatus>? statuses = await CreateHttpRepository(4).GetPluginStatuses(["Old One", "Fine", "Mine"]);

        Assert.NotNull(statuses);
        OnlinePluginStatus removed = statuses[0];
        Assert.Equal(OnlinePluginAvailability.Removed, removed.Status);
        Assert.Equal(new PluginRemovalNotice(new DateTimeOffset(2026, 11, 9, 19, 51, 4, TimeSpan.Zero), true, PluginRemovalSeverity.Critical, "Steals tokens – ☠"), removed.Removal);
        Assert.Equal(OnlinePluginAvailability.Available, statuses[1].Status);
        Assert.Null(statuses[1].Removal);
        Assert.Equal(OnlinePluginAvailability.Unknown, statuses[2].Status);
    }

    [Fact]
    public async Task GetPluginStatuses_DoesntAskARepositoryTooOldToKnow()
    {
        Assert.Null(await CreateHttpRepository(3).GetPluginStatuses(["Anything"]));
        Assert.DoesNotContain(_http.RequestedUrls, url => url.Contains("plugin/status"));
    }

    [Fact]
    public async Task GetPluginStatuses_IsNullWhenTheRepositoryFails()
    {
        _http.Add(Base + "plugin/status?names=A", Utf8("oops"), HttpStatusCode.InternalServerError);

        Assert.Null(await CreateHttpRepository(4).GetPluginStatuses(["A"]));
    }

    [Fact]
    public async Task GetPluginStatuses_AsksInChunksTheRepositoryAccepts()
    {
        string[] names = Enumerable.Range(0, RepositoryProtocol.MaxStatusNamesPerRequest + 1).Select(i => $"P{i}").ToArray();
        _http.Add(Base + "plugin/status?" + string.Join("&", names.Take(RepositoryProtocol.MaxStatusNamesPerRequest).Select(n => $"names={n}")), Utf8("[]"));
        _http.Add(Base + $"plugin/status?names={names[^1]}", Utf8("""[{"Name":"P200","Status":"Available"}]"""));

        List<OnlinePluginStatus>? statuses = await CreateHttpRepository(4).GetPluginStatuses(names);

        Assert.Equal("P200", Assert.Single(statuses!).Name);
    }

    [Theory]
    [InlineData("""[{"Name":"A","Status":"SomethingNew"}]""", OnlinePluginAvailability.Unknown)]
    [InlineData("""[{"Name":"A","Status":"1"}]""", OnlinePluginAvailability.Unknown)]
    [InlineData("""[{"Name":"A"}]""", OnlinePluginAvailability.Unknown)]
    [InlineData("""[{"name":"A","status":"removed"}]""", OnlinePluginAvailability.Removed)]
    public void ParseStatuses_ActsOnlyOnWhatItRecognises(string json, OnlinePluginAvailability expected)
    {
        Assert.Equal(expected, Assert.Single(PluginRepository.ParseStatuses(json)).Status);
    }

    [Fact]
    public void ParseStatuses_TakesAnUnknownSeverityAsTheMostSerious()
    {
        OnlinePluginStatus status = Assert.Single(PluginRepository.ParseStatuses("""[{"Name":"A","Status":"Removed","Severity":"Apocalyptic"}]"""));

        Assert.Equal(PluginRemovalSeverity.Critical, status.Removal!.Severity);
        Assert.False(status.Removal.RemovedByAdministrator);
    }

    #endregion

    #region The local database

    [Fact]
    public async Task RemovalNotice_IsKeptInTheDatabase()
    {
        LocalPlugin plugin = await InstallAsync("Leveling");
        plugin.RemovalNotice = new PluginRemovalNotice(new DateTimeOffset(2026, 11, 9, 0, 0, 0, TimeSpan.Zero), true, PluginRemovalSeverity.Warning, "Replaced");
        plugin.AcknowledgedRemovalSeverity = PluginRemovalSeverity.Notice;
        await _local.UpdatePluginAsync(plugin);

        LocalPlugin read = (await _local.GetPluginByNameAsync("Leveling"))!;

        Assert.Equal(plugin.RemovalNotice, read.RemovalNotice);
        Assert.Equal(PluginRemovalSeverity.Notice, read.AcknowledgedRemovalSeverity);
    }

    [Fact]
    public async Task ADatabaseFromAnOlderBotGetsTheNewColumns()
    {
        string oldFile = Path.Combine(_folder, "old.db");
        await using (var connection = new SqliteConnection($"Data Source={oldFile}"))
        {
            await connection.OpenAsync();
            await using SqliteCommand command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE Plugins (id TEXT PRIMARY KEY, name TEXT UNIQUE NOT NULL, version TEXT NOT NULL, file_path TEXT NOT NULL,
                                      is_offline_added INTEGER NOT NULL DEFAULT 0, is_enabled INTEGER NOT NULL DEFAULT 1);
                CREATE TABLE Dependencies (id TEXT PRIMARY KEY, name TEXT UNIQUE NOT NULL, file_path TEXT NOT NULL, plugin_id TEXT NOT NULL);
                INSERT INTO Plugins VALUES ('0193a5f0-0000-7000-8000-000000000001', 'Old', '1.0.0', 'x.dll', 0, 1);
                """;
            await command.ExecuteNonQueryAsync();
        }

        SqliteConnection.ClearAllPools();
        await using var repository = new LocalPluginRepository(oldFile);
        LocalPlugin plugin = Assert.Single(await repository.GetAllPluginsAsync());

        Assert.Equal("Old", plugin.PluginName);
        Assert.True(plugin.IsEnabled);
        Assert.Null(plugin.RemovalNotice);

        plugin.RemovalNotice = new PluginRemovalNotice(null, false, PluginRemovalSeverity.Notice, null);
        Assert.True(await repository.UpdatePluginAsync(plugin));
        Assert.Equal(PluginRemovalSeverity.Notice, (await repository.GetPluginByNameAsync("Old"))!.RemovalNotice!.Severity);
    }

    #endregion

    #region Before loading plugins

    [Fact]
    public async Task ARemovedPluginIsReportedAndKeepsRunning()
    {
        await InstallAsync("Leveling");
        RepositorySays(Removed("Leveling", PluginRemovalSeverity.Warning, reason: "Replaced by Leveling2"));

        LocalPlugin removed = Assert.Single(await CreateManager().CheckRemovedPluginsAsync());

        Assert.True(removed.IsEnabled);
        LocalPlugin saved = (await _local.GetPluginByNameAsync("Leveling"))!;
        Assert.Equal(PluginRemovalSeverity.Warning, saved.RemovalNotice!.Severity);
        _logger.Verify(l => l.Log(It.Is<string>(m => m.Contains("removed from the marketplace by its developer on 2026-11-09") && m.Contains("Replaced by Leveling2")),
            It.IsAny<object>(), LogType.Warning), Times.Once);
    }

    [Fact]
    public async Task ACriticalRemovalTurnsThePluginOffUntilItsOwnerTurnsItBackOn()
    {
        await InstallAsync("Bad");
        RepositorySays(Removed("Bad", PluginRemovalSeverity.Critical, byAdmin: true));
        PluginManager manager = CreateManager();

        await manager.CheckRemovedPluginsAsync();

        LocalPlugin saved = (await _local.GetPluginByNameAsync("Bad"))!;
        Assert.False(saved.IsEnabled);
        Assert.True(saved.IsDisabledByRemoval);
        _logger.Verify(l => l.Log(It.Is<string>(m => m.Contains("by the marketplace's administrator")), It.IsAny<object>(), LogType.Error), Times.Once);

        await manager.SetEnabledStatus("Bad", true);
        await manager.CheckRemovedPluginsAsync();

        saved = (await _local.GetPluginByNameAsync("Bad"))!;
        Assert.True(saved.IsEnabled);
        Assert.Equal(PluginRemovalSeverity.Critical, saved.AcknowledgedRemovalSeverity);
        Assert.False(saved.IsDisabledByRemoval);
    }

    [Fact]
    public async Task KeepingAPluginDespiteAWarningDoesntCoverALaterCriticalRemoval()
    {
        LocalPlugin plugin = await InstallAsync("Escalated");
        plugin.RemovalNotice = new PluginRemovalNotice(null, false, PluginRemovalSeverity.Warning, null);
        plugin.AcknowledgedRemovalSeverity = PluginRemovalSeverity.Warning;
        await _local.UpdatePluginAsync(plugin);
        RepositorySays(Removed("Escalated", PluginRemovalSeverity.Critical, byAdmin: true));

        await CreateManager().CheckRemovedPluginsAsync();

        Assert.False((await _local.GetPluginByNameAsync("Escalated"))!.IsEnabled);
    }

    [Fact]
    public async Task TheLastAnswerIsUsedWhenTheRepositoryCantBeAsked()
    {
        await InstallAsync("Bad");
        RepositorySays(Removed("Bad", PluginRemovalSeverity.Critical));
        PluginManager manager = CreateManager();
        await manager.CheckRemovedPluginsAsync();
        await manager.SetEnabledStatus("Bad", true);

        _remote.Setup(r => r.GetPluginStatuses(It.IsAny<IReadOnlyCollection<string>>())).ReturnsAsync((List<OnlinePluginStatus>?)null);
        _logger.Invocations.Clear();
        LocalPlugin removed = Assert.Single(await manager.CheckRemovedPluginsAsync());

        Assert.True(removed.IsEnabled);
        Assert.Equal(PluginRemovalSeverity.Critical, removed.RemovalNotice!.Severity);
        _logger.Verify(l => l.Log(It.Is<string>(m => m.Contains("Bad was removed")), It.IsAny<object>(), LogType.Error), Times.Once);
    }

    [Fact]
    public async Task APluginPublishedAgainIsNoLongerReported()
    {
        LocalPlugin plugin = await InstallAsync("Back");
        plugin.RemovalNotice = new PluginRemovalNotice(null, false, PluginRemovalSeverity.Critical, null);
        plugin.IsEnabled = false;
        await _local.UpdatePluginAsync(plugin);
        RepositorySays(new OnlinePluginStatus("Back", OnlinePluginAvailability.Available, null));

        Assert.Empty(await CreateManager().CheckRemovedPluginsAsync());

        LocalPlugin saved = (await _local.GetPluginByNameAsync("Back"))!;
        Assert.Null(saved.RemovalNotice);
        Assert.Null(saved.AcknowledgedRemovalSeverity);
        Assert.False(saved.IsEnabled);
    }

    [Fact]
    public async Task LocalPluginsAreNeverAskedAbout()
    {
        await InstallAsync("Handmade", offline: true);

        Assert.Empty(await CreateManager().CheckRemovedPluginsAsync());
        _remote.Verify(r => r.GetPluginStatuses(It.IsAny<IReadOnlyCollection<string>>()), Times.Never);
    }

    #endregion
}
