using DiscordBotCore.Logging;
using Moq;
using Newtonsoft.Json.Linq;
using JsonConfiguration = DiscordBotCore.Configuration.Configuration;

namespace Tests.DiscordBotCore.Configuration;

/// <summary>The JSON-file configuration the bot uses for <c>config.json</c>.</summary>
public class JsonConfigurationTests : IDisposable
{
    private readonly ILogger _logger = new Mock<ILogger>().Object;
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "seth-config-tests", Guid.NewGuid().ToString("N"));
    private readonly string _file;

    public JsonConfigurationTests()
    {
        Directory.CreateDirectory(_folder);
        _file = Path.Combine(_folder, "config.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, true);
        }
    }

    #region Loading

    [Fact]
    public void CreateFromFile_WithoutFile_CreatesAnEmptyFile()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.False(config.ContainsKey("token"));
        Assert.True(File.Exists(_file));
        Assert.Empty(JObject.Parse(File.ReadAllText(_file)));
    }

    [Fact]
    public void CreateFromFile_WithEmptyFile_StartsEmpty()
    {
        File.WriteAllText(_file, "");

        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.Null(config.FirstOrDefault().Key);
    }

    [Fact]
    public void CreateFromFile_ReadsScalarTypes()
    {
        File.WriteAllText(_file, """
            {
              "token": "abc",
              "count": 3,
              "ratio": 1.5,
              "enabled": true,
              "nothing": null
            }
            """);

        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.Equal("abc", config.Get<string>("token"));
        Assert.Equal(3, config.Get<int>("count"));
        Assert.Equal(1.5f, config.Get<float>("ratio"));
        Assert.True(config.Get<bool>("enabled"));
        Assert.True(config.ContainsKey("nothing"));
    }

    [Fact]
    public void CreateFromFile_ReadsIntegersLargerThanInt()
    {
        File.WriteAllText(_file, """{ "ServerId": 1234567890123456789 }""");

        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.Equal(1234567890123456789UL, config.Get<ulong>("ServerId"));
    }

    [Fact]
    public void CreateFromFile_ReadsListsOfSnowflakes()
    {
        File.WriteAllText(_file, """{ "ServerIds": [ 1234567890123456789, 987654321098765432 ] }""");

        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.Equal(new List<ulong> { 1234567890123456789UL, 987654321098765432UL }, config.GetList("ServerIds", new List<ulong>()));
    }

    [Fact]
    public void CreateFromFile_ReadsNestedObjectsAsDictionaries()
    {
        File.WriteAllText(_file, """
            {
              "RemotePluginConnectionConfigurationDetails": {
                "BaseUrl": "https://plugins.example.com/api/v1/",
                "PluginsEndpoint": "plugin/"
              }
            }
            """);

        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        var details = config.GetDictionary<string, string>("RemotePluginConnectionConfigurationDetails");
        Assert.Equal("https://plugins.example.com/api/v1/", details["BaseUrl"]);
        Assert.Equal("plugin/", details["PluginsEndpoint"]);
    }

    [Fact]
    public void Get_ReadsANestedObjectAsATypedDictionary()
    {
        File.WriteAllText(_file, """{ "Details": { "BaseUrl": "https://x/" } }""");

        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Dictionary<string, string>? details = config.Get<Dictionary<string, string>>("Details");
        Assert.NotNull(details);
        Assert.Equal("https://x/", details["BaseUrl"]);
    }

    [Fact]
    public void CreateFromFile_WithInvalidJson_Throws()
    {
        File.WriteAllText(_file, "{ not json");

        Assert.ThrowsAny<Newtonsoft.Json.JsonException>(() => JsonConfiguration.CreateFromFile(_logger, _file, false));
    }

    #endregion

    #region Saving

    [Fact]
    public async Task SaveToFile_RoundTripsEveryKind()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);
        config.Set("token", "secret");
        config.Set("prefix", ";");
        config.Set("AutoStart", true);
        config.Set("ServerIds", new List<ulong> { 1234567890123456789UL });
        config.Set("Details", new Dictionary<string, object> { { "BaseUrl", "https://x/" } });

        await config.SaveToFile();
        var reloaded = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.Equal("secret", reloaded.Get<string>("token"));
        Assert.Equal(";", reloaded.Get<string>("prefix"));
        Assert.True(reloaded.Get("AutoStart", false));
        Assert.Equal(new List<ulong> { 1234567890123456789UL }, reloaded.GetList("ServerIds", new List<ulong>()));
        Assert.Equal("https://x/", reloaded.GetDictionary<string, string>("Details")["BaseUrl"]);
    }

    [Fact]
    public async Task LoadFromFile_ReplacesTheInMemoryValues()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);
        config.Set("prefix", "!");
        await config.SaveToFile();
        config.Set("prefix", "?");
        config.Set("onlyInMemory", 1);

        config.LoadFromFile();

        Assert.Equal("!", config.Get<string>("prefix"));
        Assert.False(config.ContainsKey("onlyInMemory"));
    }

    #endregion

    #region Auto-add defaults

    [Fact]
    public void Get_WithAutoAdd_AddsTheDefaultForAMissingKey()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, true);

        Assert.Equal(";", config.Get("prefix", ";"));
        Assert.True(config.ContainsKey("prefix"));
    }

    [Fact]
    public void Get_WithAutoAdd_KeepsAnExistingValue()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, true);
        config.Set("prefix", "!");

        Assert.Equal("!", config.Get("prefix", ";"));
        Assert.Equal("!", config.Get<string>("prefix"));
    }

    [Fact]
    public void Get_WithAutoAdd_AcceptsANullDefault()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, true);

        Assert.Null(config.Get<string?>("missing", null));
        Assert.False(config.ContainsKey("missing"));
    }

    [Fact]
    public void Get_WithoutAutoAdd_DoesNotAddTheDefault()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, false);

        Assert.Equal(";", config.Get("prefix", ";"));
        Assert.False(config.ContainsKey("prefix"));
    }

    [Fact]
    public void GetList_WithAutoAdd_AddsTheDefaultForAMissingKey()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, true);

        Assert.Empty(config.GetList("ServerIds", new List<ulong>()));
        Assert.True(config.ContainsKey("ServerIds"));
    }

    [Fact]
    public async Task Get_WithAutoAdd_PersistsTheDefaultOnSave()
    {
        var config = JsonConfiguration.CreateFromFile(_logger, _file, true);
        config.Get("PluginFolder", "./Data/Plugins");

        await config.SaveToFile();

        Assert.Equal("./Data/Plugins", (string?)JObject.Parse(File.ReadAllText(_file))["PluginFolder"]);
    }

    #endregion
}
