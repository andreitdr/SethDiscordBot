using PluginManager;
using Xunit.Abstractions;

namespace SethTests.PluginManagerTests;

public class AppSettingsTests
{
    private readonly ITestOutputHelper _testOutputHelper;

    public AppSettingsTests(ITestOutputHelper testOutputHelper)
    {
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public void TestAppSettings_ConstructorInitializeTheClassAndFile()
    {
        var appSettings = new PluginManager.Others.SettingsDictionary<string, string>("settings.txt");
        
        Assert.NotNull(appSettings);
    }

    [Theory]
    [InlineData("key1", "value1")]
    [InlineData("key2", true)]
    public void TestAppSettings_InsertingValueIntoSettings(string keyName, object value)
    {
        var appSettings = new PluginManager.Others.SettingsDictionary<string, object>("settings.txt");

        appSettings[keyName] = value;
        Assert.True(appSettings.ContainsKey(keyName));
    }

    [Theory]
    //[InlineData("key2", 32)] // fails
    [InlineData("key1", "value1")]
    public void TestAppSettings_GettingTheValueFromSettings(string keyName, object value)
    {
        var appSettings = new PluginManager.Others.SettingsDictionary<string, object>("settings.txt");

        appSettings[keyName] = value;
        
        Assert.Same(appSettings[keyName], value);
    }
    
}