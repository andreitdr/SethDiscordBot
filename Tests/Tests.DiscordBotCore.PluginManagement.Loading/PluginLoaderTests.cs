using System.Runtime.CompilerServices;
using Discord.WebSocket;
using DiscordBotCore.Commands;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Loading;
using DiscordBotCore.PluginManagement.Models;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement.Loading;

/// <summary>Loads the fixture plugin (Tests/TestPlugins/Tests.FixturePlugin) as the bot loads real plugins.</summary>
public class PluginLoaderTests : IDisposable
{
    private static readonly string FixtureSource = Path.Combine(AppContext.BaseDirectory, "TestPlugins", "Tests.FixturePlugin.dll");

    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-loader-tests", Guid.NewGuid().ToString("N"));
    private readonly string _resources;
    private readonly string _fixturePath;
    private readonly Mock<IPluginManager> _pluginManager = new();
    private readonly Mock<IConfiguration> _configuration = new();
    private readonly List<(string Message, LogType Type)> _logs = new();
    private readonly List<LocalPlugin> _installed = new();

    public PluginLoaderTests()
    {
        _resources = Path.Combine(_folder, "Resources");
        Directory.CreateDirectory(_resources);
        // Each test loads its own copy, so deleting or replacing it can't affect other tests
        _fixturePath = Path.Combine(_folder, "Tests.FixturePlugin.dll");
        File.Copy(FixtureSource, _fixturePath);

        _pluginManager.Setup(m => m.GetInstalledPlugins()).ReturnsAsync(() => _installed.ToList());
        _configuration.Setup(c => c.Get<string>("prefix")).Returns(";");
        _configuration.Setup(c => c.Get<string>("ResourcesFolder")).Returns(_resources);
        _configuration.Setup(c => c.GetList("ServerIds", It.IsAny<List<ulong>>())).Returns(new List<ulong>());

        // The built-in help command is found among the loaded DiscordBotCore assemblies
        RuntimeHelpers.RunClassConstructor(typeof(HelpCommand).TypeHandle);
    }

    public void Dispose()
    {
        Directory.Delete(_folder, true);
    }

    private PluginLoader CreateLoader()
    {
        var logger = new Mock<ILogger>();
        logger.Setup(l => l.Log(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<LogType>()))
              .Callback<string, object, LogType>((message, _, type) => _logs.Add((message, type)));
        logger.Setup(l => l.Log(It.IsAny<string>(), It.IsAny<object>()))
              .Callback<string, object>((message, _) => _logs.Add((message, LogType.Info)));
        return new PluginLoader(_pluginManager.Object, logger.Object, _configuration.Object);
    }

    private void Install(string name, string? path = null, bool enabled = true) =>
        _installed.Add(new LocalPlugin { Id = Guid.NewGuid(), PluginName = name, PluginVersion = "1.0.0", FilePath = path ?? _fixturePath, IsEnabled = enabled });

    private bool Logged(LogType type, string fragment) => _logs.Any(l => l.Type == type && l.Message.Contains(fragment));

    private static T Read<T>(object instance, string property) => (T)instance.GetType().GetProperty(property)!.GetValue(instance)!;

    #region Loading

    [Fact]
    public async Task LoadPlugins_WithoutPlugins_LoadsOnlyTheHelpCommand()
    {
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.True(loader.IsLoaded);
        Assert.Equal("help", Assert.Single(loader.Commands).Command);
        Assert.Empty(loader.Events);
        Assert.Empty(loader.SlashCommands);
        Assert.Empty(loader.LoadedPlugins);
        Assert.True(Logged(LogType.Info, "No plugins installed"));
    }

    [Fact]
    public async Task LoadPlugins_LoadsTheCommandsOfAnEnabledPlugin()
    {
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.Equal(["fixture", "fixture-admin", "help"], loader.Commands.Select(c => c.Command).Order());
        IDbCommand fixture = loader.Commands.Single(c => c.Command == "fixture");
        Assert.Equal(["fx"], fixture.Aliases);
        Assert.True(loader.Commands.Single(c => c.Command == "fixture-admin").RequireAdmin);

        LoadedPluginAssembly loaded = Assert.Single(loader.LoadedPlugins);
        Assert.Equal("Fixture", loaded.Plugin.PluginName);
        Assert.Equal("Tests.FixturePlugin", loaded.Assembly.GetName().Name);
    }

    [Fact]
    public async Task LoadPlugins_SkipsAbstractTypesAndLogsBrokenConstructors()
    {
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.DoesNotContain(loader.Commands, c => c.Command is "abstract" or "broken");
        Assert.True(Logged(LogType.Error, "BrokenCommand"));
        Assert.True(Logged(LogType.Error, "broken on purpose"));
    }

    [Fact]
    public async Task LoadPlugins_IgnoresDisabledPlugins()
    {
        Install("Disabled", Path.Combine(_folder, "missing.dll"), enabled: false);
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.Empty(loader.LoadedPlugins);
        Assert.True(Logged(LogType.Info, "No enabled plugins"));
        Assert.False(Logged(LogType.Error, "missing.dll"));
    }

    [Fact]
    public async Task LoadPlugins_LogsBrokenEntriesAndLoadsTheRest()
    {
        string notADll = Path.Combine(_folder, "garbage.dll");
        await File.WriteAllTextAsync(notADll, "not an assembly");
        Install("Missing", Path.Combine(_folder, "missing.dll"));
        Install("Garbage", notADll);
        Install("NoPath", "");
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.Equal("Fixture", Assert.Single(loader.LoadedPlugins).Plugin.PluginName);
        Assert.True(Logged(LogType.Error, "missing.dll does not exist"));
        Assert.True(Logged(LogType.Error, "Garbage: failed to load"));
        Assert.True(Logged(LogType.Error, "NoPath has no file path"));
    }

    [Fact]
    public async Task LoadPlugins_LeavesThePluginFileUnlocked()
    {
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();

        // Loaded from memory, so the file can be replaced while the plugin runs
        File.Delete(_fixturePath);
        Assert.False(File.Exists(_fixturePath));
        Assert.Contains(loader.Commands, c => c.Command == "fixture");
    }

    [Fact]
    public async Task LoadPlugins_Twice_DoesNotDuplicateAnything()
    {
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();
        await loader.LoadPlugins();

        Assert.Equal(3, loader.Commands.Count);
        Assert.True(Logged(LogType.Warning, "already loaded"));
    }

    [Fact]
    public async Task LoadPlugins_RaisesPluginsLoadedAfterEverythingIsLoaded()
    {
        Install("Fixture");
        var loader = CreateLoader();
        int commandsWhenRaised = -1;
        loader.PluginsLoaded += () => commandsWhenRaised = loader.Commands.Count;

        await loader.LoadPlugins();

        Assert.Equal(3, commandsWhenRaised);
    }

    [Fact]
    public async Task LoadPlugins_AFailingHandlerDoesNotStopTheOthers()
    {
        var loader = CreateLoader();
        bool secondCalled = false;
        loader.PluginsLoaded += () => throw new InvalidOperationException("handler failed");
        loader.PluginsLoaded += () => secondCalled = true;

        await loader.LoadPlugins();

        Assert.True(secondCalled);
        Assert.True(Logged(LogType.Error, "handler failed"));
    }

    #endregion

    #region Events and slash commands

    [Fact]
    public async Task Events_WithoutAClient_AreSkipped()
    {
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.Empty(loader.Events);
        Assert.True(Logged(LogType.Warning, "Skipping event Fixture event"));
    }

    [Fact]
    public async Task Events_WithAClient_StartWithThePrefixAndTheirOwnFolder()
    {
        Install("Fixture");
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);

        await loader.LoadPlugins();

        Assert.Equal(["Event without stop", "Fixture event"], loader.Events.Select(e => e.Name).Order());
        IDbEvent fixture = loader.Events.Single(e => e.Name == "Fixture event");
        Assert.Equal(1, Read<int>(fixture, "StartCount"));
        Assert.Equal(";", Read<string>(fixture, "BotPrefix"));
        string expectedFolder = Path.Combine(_resources, "Tests.FixturePlugin");
        Assert.Equal(Path.GetFullPath(expectedFolder), Read<string>(fixture, "PluginBaseDirectory"));
        Assert.True(Directory.Exists(expectedFolder));
    }

    [Fact]
    public async Task Events_ThatFailToStart_AreLoggedAndSkipped()
    {
        Install("Fixture");
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);

        await loader.LoadPlugins();

        Assert.DoesNotContain(loader.Events, e => e.Name == "Failing event");
        Assert.True(Logged(LogType.Error, "Event Failing event failed to start: fails on purpose"));
    }

    [Fact]
    public async Task Events_NeedThePrefixAndTheResourcesFolder()
    {
        Install("Fixture");
        _configuration.Setup(c => c.Get<string>("prefix")).Returns((string?)null);
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);

        await loader.LoadPlugins();

        Assert.Empty(loader.Events);
        Assert.True(Logged(LogType.Error, "Bot prefix is not set"));
    }

    [Fact]
    public async Task SlashCommands_WithoutAClient_AreSkipped()
    {
        Install("Fixture");
        var loader = CreateLoader();

        await loader.LoadPlugins();

        Assert.Empty(loader.SlashCommands);
        Assert.True(Logged(LogType.Warning, "Skipping 1 slash command(s): the Discord client is not set"));
    }

    [Fact]
    public async Task SlashCommands_WithoutGuilds_AreSkipped()
    {
        Install("Fixture");
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);

        await loader.LoadPlugins();

        Assert.Empty(loader.SlashCommands);
        Assert.True(Logged(LogType.Warning, "the bot is not in any guild"));
    }

    [Fact]
    public void SetDiscordClient_RejectsAClientThatIsNotLoggedIn()
    {
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();

        loader.SetDiscordClient(client);

        Assert.True(Logged(LogType.Error, "must be logged in"));
    }

    #endregion

    #region Unloading

    [Fact]
    public async Task UnloadAllPlugins_StopsEventsAndClearsEverything()
    {
        Install("Fixture");
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);
        await loader.LoadPlugins();
        IDbEvent fixture = loader.Events.Single(e => e.Name == "Fixture event");
        Assert.True(Read<bool>(fixture, "HandlerAttached"));

        await loader.UnloadAllPlugins();

        Assert.Equal(1, Read<int>(fixture, "StopCount"));
        Assert.False(Read<bool>(fixture, "HandlerAttached"));
        Assert.False(loader.IsLoaded);
        Assert.Empty(loader.Commands);
        Assert.Empty(loader.Events);
        Assert.Empty(loader.SlashCommands);
        Assert.Empty(loader.LoadedPlugins);
    }

    [Fact]
    public async Task Reload_StartsEachEventOnceAndStopsTheOldOnes()
    {
        Install("Fixture");
        var loader = CreateLoader();
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);
        await loader.LoadPlugins();
        IDbEvent first = loader.Events.Single(e => e.Name == "Fixture event");

        await loader.UnloadAllPlugins();
        await loader.LoadPlugins();

        IDbEvent second = loader.Events.Single(e => e.Name == "Fixture event");
        Assert.NotSame(first, second);
        Assert.Equal(1, Read<int>(first, "StopCount"));
        Assert.False(Read<bool>(first, "HandlerAttached"));
        Assert.Equal(1, Read<int>(second, "StartCount"));
        Assert.Equal(0, Read<int>(second, "StopCount"));
    }

    [Fact]
    public async Task UnloadAllPlugins_RaisesPluginsUnloadingWhileThePluginsAreStillThere()
    {
        Install("Fixture");
        var loader = CreateLoader();
        await loader.LoadPlugins();
        int pluginsWhenRaised = -1;
        loader.PluginsUnloading += () => pluginsWhenRaised = loader.LoadedPlugins.Count;

        await loader.UnloadAllPlugins();

        Assert.Equal(1, pluginsWhenRaised);
    }

    [Fact]
    public async Task UnloadAllPlugins_WhenNothingIsLoaded_DoesNothing()
    {
        var loader = CreateLoader();
        bool raised = false;
        loader.PluginsUnloading += () => raised = true;

        await loader.UnloadAllPlugins();

        Assert.False(raised);
    }

    [Fact]
    public async Task UnloadAllPlugins_LetsTheLoadContextBeCollected()
    {
        Install("Fixture");
        var loader = CreateLoader();
        WeakReference assembly = await LoadAndUnloadAsync(loader);

        for (int i = 0; i < 20 && assembly.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(10);
        }

        Assert.False(assembly.IsAlive, "The plugin assembly was not collected; something still references a plugin type.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> LoadAndUnloadAsync(PluginLoader loader)
    {
        using var client = new DiscordSocketClient();
        loader.SetDiscordClientWithoutLoginCheck(client);
        await loader.LoadPlugins();
        var reference = new WeakReference(loader.LoadedPlugins.Single().Assembly);
        await loader.UnloadAllPlugins();
        loader.ClearDiscordClient();
        return reference;
    }

    #endregion
}
