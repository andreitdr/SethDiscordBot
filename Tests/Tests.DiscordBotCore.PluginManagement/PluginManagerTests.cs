using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement;

public class PluginManagerTests
{
    private readonly Mock<IPluginRepository> _pluginRepositoryMock = new();
    private readonly Mock<ILocalPluginRepository> _localPluginRepositoryMock = new();

    private PluginManager CreatePluginManager() =>
        new(_pluginRepositoryMock.Object, _localPluginRepositoryMock.Object, new Mock<ILogger>().Object, new Mock<IConfiguration>().Object);

    #region IsNewVersion

    [Theory]
    [InlineData("1.0.0", "1.0.1")]
    [InlineData("1.0.9", "1.1.0")]
    [InlineData("1.9.9", "2.0.0")]
    [InlineData("1.0.9", "1.0.10")]
    public void IsNewVersion_ShouldSucceedForNewerVersion(string current, string next)
    {
        Assert.True(PluginManager.IsNewVersion(current, next).IsSuccess);
    }

    [Theory]
    [InlineData("1.0.1", "1.0.0", "Current version is newer")]
    [InlineData("2.0.0", "1.9.9", "Current version is newer")]
    [InlineData("1.0.0", "1.0.0", "Versions are the same")]
    public void IsNewVersion_ShouldFailForSameOrOlderVersion(string current, string next, string message)
    {
        var response = PluginManager.IsNewVersion(current, next);
        Assert.False(response.IsSuccess);
        Assert.Equal(message, response.Message);
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1-beta")]
    [InlineData("1.0.0-beta", "1.0.1")]
    [InlineData("1.0.0", "1.0")]
    [InlineData("1.0.0", "1.0.0.0")]
    [InlineData("1.0.0", "a.b.c")]
    [InlineData("1.0.0", "")]
    [InlineData("1.0.0", "-1.0.0")]
    public void IsNewVersion_ShouldFailWithoutThrowingForInvalidVersion(string current, string next)
    {
        var response = PluginManager.IsNewVersion(current, next);
        Assert.False(response.IsSuccess);
        Assert.StartsWith("Invalid", response.Message);
    }

    #endregion

    #region Libraries folder

    [Theory]
    [InlineData("MyPlugin")]
    [InlineData("My Plugin")]
    [InlineData("Group/MyPlugin")]
    public void GetPluginLibrariesFolder_ShouldStayInsideLibraries(string pluginName)
    {
        string root = Path.Combine(Path.GetTempPath(), "Libraries");
        string? folder = PluginManager.GetPluginLibrariesFolder(root, pluginName);

        Assert.NotNull(folder);
        Assert.Equal(Path.GetFullPath(Path.Combine(root, pluginName)), folder);
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("..")]
    [InlineData("../Other")]
    [InlineData("MyPlugin/../..")]
    [InlineData("/etc")]
    public void GetPluginLibrariesFolder_ShouldRejectNamesOutsideLibraries(string pluginName)
    {
        string root = Path.Combine(Path.GetTempPath(), "Libraries");
        Assert.Null(PluginManager.GetPluginLibrariesFolder(root, pluginName));
    }

    [Fact]
    public async Task UninstallPluginByName_ShouldDeleteLibrariesRecursively()
    {
        // PluginManager resolves ./Libraries from the working directory
        string pluginName = $"UninstallTest_{Guid.NewGuid():N}";
        string librariesFolder = Path.GetFullPath(Path.Combine("Libraries", pluginName));
        string pluginFile = Path.GetTempFileName();
        Directory.CreateDirectory(Path.Combine(librariesFolder, "runtimes", "native"));
        File.WriteAllText(Path.Combine(librariesFolder, "top.dll"), "");
        File.WriteAllText(Path.Combine(librariesFolder, "runtimes", "native", "lib.so"), "");

        var plugin = new LocalPlugin { Id = Guid.NewGuid(), PluginName = pluginName, PluginVersion = "1.0.0", FilePath = pluginFile };
        _localPluginRepositoryMock.Setup(r => r.GetPluginByNameAsync(pluginName)).ReturnsAsync(plugin);

        try
        {
            var response = await CreatePluginManager().UninstallPluginByName(pluginName);

            Assert.True(response.IsSuccess);
            Assert.False(Directory.Exists(librariesFolder));
            Assert.False(File.Exists(pluginFile));
            _localPluginRepositoryMock.Verify(r => r.DeletePluginAsync(plugin.Id), Times.Once);
        }
        finally
        {
            if (Directory.Exists(librariesFolder))
            {
                Directory.Delete(librariesFolder, true);
            }
        }
    }

    #endregion

    #region Repository not configured

    [Fact]
    public async Task UnconfiguredRepository_ShouldNotQueryTheRepository()
    {
        _pluginRepositoryMock.Setup(r => r.IsConfigured).Returns(false);
        var pluginManager = CreatePluginManager();

        Assert.False(pluginManager.IsRepositoryConfigured);
        Assert.Empty(await pluginManager.GetOnlinePluginsList());
        Assert.False((await pluginManager.GetOnlinePluginDataById(1)).IsSuccess);
        Assert.False((await pluginManager.GetOnlinePluginDataByName("Plugin")).IsSuccess);

        _pluginRepositoryMock.Verify(r => r.GetAllPlugins(It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
        _pluginRepositoryMock.Verify(r => r.GetPluginById(It.IsAny<int>()), Times.Never);
        _pluginRepositoryMock.Verify(r => r.GetPluginByName(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<bool>()), Times.Never);
    }

    #endregion
}
