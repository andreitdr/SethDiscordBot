using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Helpers;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement;

public class PluginRepositoryTests
{
    [Fact]
    public void Default_ShouldNotBeConfigured()
    {
        Assert.False(PluginRepositoryConfiguration.Default.IsConfigured);
    }

    [Theory]
    [InlineData("http://plugins.example.com/api/v1/", true)]
    [InlineData("https://plugins.example.com/api/v1/", true)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("plugins.example.com/api/v1/", false)]
    [InlineData("ftp://plugins.example.com/", false)]
    public void IsConfigured_ShouldRequireAbsoluteHttpUrl(string baseUrl, bool expected)
    {
        var configuration = new PluginRepositoryConfiguration(baseUrl, "plugin/", "dependency/");
        Assert.Equal(expected, configuration.IsConfigured);
    }

    [Fact]
    public async Task UnconfiguredRepository_ShouldReturnNothingWithoutRequesting()
    {
        // With no BaseAddress, any request would throw InvalidOperationException
        var repository = new PluginRepository(PluginRepositoryConfiguration.Default, new Mock<ILogger>().Object);

        Assert.False(repository.IsConfigured);
        Assert.Empty(await repository.GetAllPlugins(1, false));
        Assert.Null(await repository.GetPluginById(1));
        Assert.Null(await repository.GetPluginByName("Plugin", 1, false));
        Assert.Empty(await repository.GetDependenciesForPlugin(1));
    }

    [Fact]
    public void CreateUrlWithQueryParams_ShouldJoinParametersInOrder()
    {
        string url = PluginRepository.CreateUrlWithQueryParams("plugin/", "get-all-plugins", new Dictionary<string, string>
        {
            { "operatingSystem", "1" },
            { "includeNotApproved", false.ToString() }
        });

        Assert.Equal("plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", url);
    }

    [Fact]
    public void CreateUrlWithQueryParams_ShouldEscapeKeysAndValues()
    {
        string url = PluginRepository.CreateUrlWithQueryParams("plugin/", "get-by-name", new Dictionary<string, string>
        {
            { "pluginName", "My Plugin&x=1/é" },
            { "a b", "?" }
        });

        Assert.Equal("plugin/get-by-name?pluginName=My%20Plugin%26x%3D1%2F%C3%A9&a%20b=%3F", url);
    }

    [Fact]
    public void CreateUrlWithQueryParams_ShouldOmitQuestionMarkWithoutParameters()
    {
        string url = PluginRepository.CreateUrlWithQueryParams("plugin/", "get-all-plugins", new Dictionary<string, string>());
        Assert.Equal("plugin/get-all-plugins", url);
    }
}
