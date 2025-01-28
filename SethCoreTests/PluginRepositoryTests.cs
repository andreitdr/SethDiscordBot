using System.Net;
using DiscordBotCore.Interfaces.PluginManagement;
using DiscordBotCore.Online.Helpers;
using Moq;
using Moq.Protected;

namespace SethCoreTests;

public class PluginRepositoryTests
{
    private readonly Mock<IPluginRepositoryConfiguration> _mockConfig;
    private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
    private readonly PluginRepository _pluginRepository;

    public PluginRepositoryTests()
    {
        _mockConfig = new Mock<IPluginRepositoryConfiguration>();
        _mockConfig.SetupGet(c => c.BaseUrl).Returns("http://localhost/");
        _mockConfig.SetupGet(c => c.PluginRepositoryLocation).Returns("api/plugins/");
        _mockConfig.SetupGet(c => c.DependenciesRepositoryLocation).Returns("api/dependencies/");

        _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(_mockHttpMessageHandler.Object)
        {
            BaseAddress = new System.Uri(_mockConfig.Object.BaseUrl)
        };

        _pluginRepository = new PluginRepository(_mockConfig.Object)
        {
            _httpClient = httpClient
        };
    }

    [Fact]
    public async Task GetAllPlugins_ReturnsListOfPlugins()
    {
        var pluginsJson = "[{\"PluginId\":1,\"PluginName\":\"TestPlugin\",\"PluginDescription\":\"Description\",\"LatestVersion\":\"1.0\",\"PluginAuthor\":\"Author\",\"PluginLink\":\"http://link\",\"OperatingSystem\":1}]";
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(pluginsJson)
            });
        
        var result = await _pluginRepository.GetAllPlugins();

        Assert.Single(result);
        Assert.Equal("TestPlugin", result[0].PluginName);
    }

    [Fact]
    public async Task GetPluginById_ReturnsPlugin()
    {
        var pluginJson = "{\"PluginId\":1,\"PluginName\":\"TestPlugin\",\"PluginDescription\":\"Description\",\"LatestVersion\":\"1.0\",\"PluginAuthor\":\"Author\",\"PluginLink\":\"http://link\",\"OperatingSystem\":1}";
        _mockHttpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(pluginJson)
            });

        var result = await _pluginRepository.GetPluginById(1);
        
        Assert.NotNull(result);
        Assert.Equal("TestPlugin", result.PluginName);
    }
}