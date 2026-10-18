using System.Net;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement;

/// <summary>The marketplace client against a fake repository that speaks the documented API.</summary>
public class PluginRepositoryHttpTests
{
    private const string Base = "https://plugins.example.com/api/v1/";

    private const string PluginJson = """
        {
          "id": 7,
          "name": "LevelingSystem",
          "description": "Gives users experience for chatting – café ☕",
          "version": "1.0.0",
          "author": "andreitdr",
          "downloadLink": "https://plugins.example.com/files/LevelingSystem.dll",
          "operatingSystem": 1,
          "isApproved": true
        }
        """;

    private const string DependencyJson = """
        {
          "dependencyName": "SomeLibrary",
          "dependencyLink": "https://plugins.example.com/files/SomeLibrary.dll",
          "dependencyLocation": "sub/SomeLibrary.dll",
          "isExecutable": true
        }
        """;

    private readonly FakeHttpHandler _http = new();

    private PluginRepository CreateRepository() =>
        new(new PluginRepositoryConfiguration(Base, "plugin/", "dependency/"), new Mock<ILogger>().Object, new HttpClient(_http));

    private void Respond(string pathAndQuery, string json, HttpStatusCode status = HttpStatusCode.OK) =>
        _http.Add(Base + pathAndQuery, System.Text.Encoding.UTF8.GetBytes(json), status);

    [Fact]
    public async Task GetAllPlugins_ParsesTheDocumentedPluginObject()
    {
        Respond("plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", $"[{PluginJson}]");

        OnlinePlugin plugin = Assert.Single(await CreateRepository().GetAllPlugins(1, false));

        Assert.Equal(7, plugin.Id);
        Assert.Equal("LevelingSystem", plugin.Name);
        Assert.Equal("Gives users experience for chatting – café ☕", plugin.Description);
        Assert.Equal("1.0.0", plugin.Version);
        Assert.Equal("andreitdr", plugin.Author);
        Assert.Equal("https://plugins.example.com/files/LevelingSystem.dll", plugin.DownloadLink);
        Assert.Equal(1, plugin.OperatingSystem);
        Assert.True(plugin.IsApproved);
    }

    [Fact]
    public async Task GetPluginById_UsesTheDocumentedUrl()
    {
        Respond("plugin/get-by-id?pluginId=7&includeNotApproved=false", PluginJson);

        Assert.Equal("LevelingSystem", (await CreateRepository().GetPluginById(7))?.Name);
    }

    [Fact]
    public async Task GetPluginByName_EscapesTheName()
    {
        Respond("plugin/get-by-name?pluginName=My%20Plugin&operatingSystem=2&includeNotApproved=True", PluginJson);

        Assert.NotNull(await CreateRepository().GetPluginByName("My Plugin", 2, true));
    }

    [Fact]
    public async Task GetDependenciesForPlugin_ParsesTheDocumentedDependencyObject()
    {
        Respond("dependency/get-by-plugin-id?pluginId=7", $"[{DependencyJson}]");

        OnlineDependencyInfo dependency = Assert.Single(await CreateRepository().GetDependenciesForPlugin(7));

        Assert.Equal("SomeLibrary", dependency.DependencyName);
        Assert.Equal("https://plugins.example.com/files/SomeLibrary.dll", dependency.DownloadLink);
        Assert.Equal("sub/SomeLibrary.dll", dependency.DownloadLocation);
        Assert.True(dependency.IsExecutable);
    }

    [Fact]
    public async Task ErrorStatus_MeansNothingFound()
    {
        var repository = CreateRepository();

        Assert.Empty(await repository.GetAllPlugins(1, false));
        Assert.Null(await repository.GetPluginById(7));
        Assert.Null(await repository.GetPluginByName("X", 1, false));
        Assert.Empty(await repository.GetDependenciesForPlugin(7));
    }

    [Fact]
    public async Task InvalidJson_MeansNothingFoundInsteadOfThrowing()
    {
        Respond("plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", "<html>maintenance</html>");
        Respond("plugin/get-by-id?pluginId=7&includeNotApproved=false", "{ broken");
        Respond("dependency/get-by-plugin-id?pluginId=7", "not json");

        var repository = CreateRepository();

        Assert.Empty(await repository.GetAllPlugins(1, false));
        Assert.Null(await repository.GetPluginById(7));
        Assert.Empty(await repository.GetDependenciesForPlugin(7));
    }

    [Fact]
    public async Task UnreachableServer_MeansNothingFound()
    {
        var repository = new PluginRepository(new PluginRepositoryConfiguration(Base, "plugin/", "dependency/"),
            new Mock<ILogger>().Object, new HttpClient(new ThrowingHandler()));

        Assert.Empty(await repository.GetAllPlugins(1, false));
        Assert.Null(await repository.GetPluginByName("X", 1, false));
    }

    private sealed class ThrowingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }
}
