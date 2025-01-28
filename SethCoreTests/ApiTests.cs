using DiscordBotCore;
using DiscordBotCore.API.Endpoints.PluginManagement;
using DiscordBotCore.Online;
using DiscordBotCore.Plugin;
using Moq;

namespace SethCoreTests;

public class PluginInstallEndpointTests
{
    private readonly Mock<IPluginManager> _mockPluginManager;
    private readonly PluginInstallEndpoint _endpoint;

    public PluginInstallEndpointTests()
    {
        _mockPluginManager = new Mock<IPluginManager>();
        _endpoint = new PluginInstallEndpoint(_mockPluginManager.Object);
    }

    [Fact]
    public async Task HandleRequest_SuccessfulPluginInstallation_ReturnsOk()
    {
        var pluginName = "TestPlugin";
        var pluginInfo = new OnlinePlugin(1, pluginName, "Description", "1.0", "Author", "http://link", 1);
        _mockPluginManager.Setup(pm => pm.GetPluginDataByName(pluginName)).ReturnsAsync(pluginInfo);
        _mockPluginManager.Setup(pm => pm.InstallPluginNoProgress(pluginInfo)).Returns(Task.CompletedTask);

        var jsonRequest = $"{{\"pluginName\":\"{pluginName}\"}}";
        var response = await _endpoint.HandleRequest(jsonRequest);

        Assert.True(response.Success);
    }

    [Fact]
    public async Task HandleRequest_PluginNotFound_ReturnsFail()
    {
        var pluginName = "NonExistentPlugin";
        _mockPluginManager.Setup(pm => pm.GetPluginDataByName(pluginName)).ReturnsAsync((OnlinePlugin?)null);

        var jsonRequest = $"{{\"pluginName\":\"{pluginName}\"}}";
        var response = await _endpoint.HandleRequest(jsonRequest);

        Assert.False(response.Success);
    }
}