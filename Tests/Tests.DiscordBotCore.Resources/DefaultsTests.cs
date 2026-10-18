using DiscordBotCore.Resources;

namespace Tests.DiscordBotCore.Resources;

/// <summary>
/// The default paths and the configuration key names. Users' config.json files and Docker volumes depend on them, so
/// a rename would silently lose settings or data.
/// </summary>
public class DefaultsTests
{
    public static TheoryData<string> DefaultPaths => new()
    {
        DefaultConfigurationValues.DefaultLogFolder,
        DefaultConfigurationValues.DefaultResourcesFolder,
        DefaultConfigurationValues.DefaultConfigFile,
        DefaultConfigurationValues.DefaultPluginFolder,
        DefaultConfigurationValues.DefaultPluginDatabaseFile,
    };

    [Theory]
    [MemberData(nameof(DefaultPaths))]
    public void DefaultPaths_AreAllUnderTheDataFolder(string path)
    {
        Assert.StartsWith("./Data/", path);
    }

    [Fact]
    public void ConfigAndPluginDatabase_AreInTheResourcesFolder()
    {
        string resources = DefaultConfigurationValues.DefaultResourcesFolder + "/";

        Assert.StartsWith(resources, DefaultConfigurationValues.DefaultConfigFile);
        Assert.StartsWith(resources, DefaultConfigurationValues.DefaultPluginDatabaseFile);
        Assert.EndsWith("config.json", DefaultConfigurationValues.DefaultConfigFile);
        Assert.EndsWith("plugins.db", DefaultConfigurationValues.DefaultPluginDatabaseFile);
    }

    [Fact]
    public void DefaultMaxHistorySize_IsAPositiveNumber()
    {
        Assert.True(int.TryParse(DefaultConfigurationValues.DefaultMaxHistorySize, out int size));
        Assert.True(size > 0);
    }

    [Fact]
    public void DefaultLogFormat_UsesKnownPlaceholders()
    {
        Assert.Equal("{ThrowTime} {SenderName} {Message}", DefaultConfigurationValues.DefaultLogFormat);
    }

    [Fact]
    public void ConfigurationKeys_MatchTheDocumentedNames()
    {
        Assert.Equal("PluginFolder", DefaultConfigurationKeys.PluginFolder);
        Assert.Equal("ResourcesFolder", DefaultConfigurationKeys.ResourcesFolder);
        Assert.Equal("PluginDatabase", DefaultConfigurationKeys.PluginsDatabaseFile);
        Assert.Equal("RemotePluginConnectionConfigurationDetails", DefaultConfigurationKeys.RemotePluginConnectionConfiguration.Details);
        Assert.Equal("BaseUrl", DefaultConfigurationKeys.RemotePluginConnectionConfiguration.BaseUrl);
        Assert.Equal("PluginsEndpoint", DefaultConfigurationKeys.RemotePluginConnectionConfiguration.PluginsEndpoint);
        Assert.Equal("DependenciesEndpoint", DefaultConfigurationKeys.RemotePluginConnectionConfiguration.DependenciesEndpoint);
    }
}
