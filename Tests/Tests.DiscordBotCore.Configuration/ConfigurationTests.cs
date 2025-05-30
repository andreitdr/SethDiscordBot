using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using Moq;

namespace Tests.DiscordBotCore.Configuration;

public class ConfigurationTests
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly string _testFilePath;

    public ConfigurationTests()
    {
        _loggerMock = new Mock<ILogger>();
        _testFilePath = Path.GetTempFileName();
    }

    #region Basic Operations

    [Fact]
    public void Add_ShouldAddKeyValuePair()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 100);
        Assert.True(config.ContainsKey("Key1"));
        Assert.Equal(100, config.Get("Key1"));
    }

    [Fact]
    public void Add_ShouldIgnoreNullKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", null);
        Assert.False(config.ContainsKey("Key1"));
    }

    [Fact]
    public void Set_ShouldOverrideValue()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 1);
        config.Set("Key1", 2);
        Assert.Equal(2, config.Get("Key1"));
    }

    [Fact]
    public void Remove_ShouldDeleteKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 1);
        config.Remove("Key1");
        Assert.False(config.ContainsKey("Key1"));
    }

    [Fact]
    public void Clear_ShouldEmptyDictionary()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 1);
        config.Clear();
        Assert.False(config.ContainsKey("Key1"));
    }

    #endregion

    #region Retrieval

    [Fact]
    public void Get_ShouldReturnStoredValue()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 123);
        Assert.Equal(123, config.Get<int>("Key1"));
    }

    [Fact]
    public void Get_WithDefault_ShouldReturnStoredValue()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 50);
        Assert.Equal(50, config.Get("Key1", 100));
    }

    [Fact]
    public void Get_WithDefault_ShouldReturnDefaultWithoutAutoAddIfDisabled()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        int result = config.Get("MissingKey", 300);
        Assert.Equal(300, result);
        Assert.False(config.ContainsKey("MissingKey"));
    }

    [Fact]
    public void TryGetValue_ShouldReturnTrueIfKeyExists()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 5);
        bool result = config.TryGetValue("Key1", out object? val);
        Assert.True(result);
        Assert.Equal(5, val);
    }

    [Fact]
    public void TryGetValue_ShouldReturnFalseIfKeyDoesNotExist()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        bool result = config.TryGetValue("KeyX", out object? val);
        Assert.False(result);
        Assert.Null(val);
    }

    #endregion

    #region Type Conversion and Complex Types

    [Fact]
    public void Get_WithTypeConversion_ShouldReturnConvertedValue()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", "123");
        int result = config.Get<int>("Key1");
        Assert.Equal(123, result);
    }

    [Fact]
    public void GetList_ShouldReturnStoredList()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        var expected = new List<int> { 1, 2, 3 };
        config.Set("KeyList", expected);
        var result = config.GetList("KeyList", new List<int>());
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GetList_ShouldReturnDefaultAndLogIfMissing()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        var defaultList = new List<int> { 10 };
        var result = config.GetList("MissingList", defaultList);
        Assert.Equal(defaultList, result);
        _loggerMock.Verify(log => log.Log(It.Is<string>(s => s.Contains("Key 'MissingList' not found")), LogType.Warning), Times.Once);
    }

    [Fact]
    public void GetDictionary_ShouldReturnStoredDictionary()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        var dict = new Dictionary<string, object> { { "a", 1 }, { "b", 2 } };
        config.Set("DictKey", dict);
        var result = config.GetDictionary<string, int>("DictKey");
        Assert.Equal(2, result.Count);
        Assert.Equal(1, result["a"]);
    }

    [Fact]
    public void GetDictionary_ShouldThrowIfNotDictionary()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Set("NotDict", 123);
        Assert.Throws<Exception>(() => config.GetDictionary<string, int>("NotDict"));
    }

    #endregion

    #region LINQ Operations

    [Fact]
    public void ContainsKey_ShouldReturnTrueForExistingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 1);
        Assert.True(config.ContainsKey("Key1"));
    }

    [Fact]
    public void ContainsKey_ShouldReturnFalseForMissingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        Assert.False(config.ContainsKey("KeyX"));
    }

    [Fact]
    public void ContainsAllKeys_ShouldReturnTrueIfAllKeysExist()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);
        Assert.True(config.ContainsAllKeys("A", "B"));
    }

    [Fact]
    public void ContainsAllKeys_ShouldReturnFalseIfAnyKeyMissing()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        Assert.False(config.ContainsAllKeys("A", "C"));
    }

    [Fact]
    public void Where_WithPredicate_ShouldFilterResults()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);
        var result = config.Where(pair => (int)pair.Value > 1);
        Assert.Single(result);
    }

    [Fact]
    public void Where_WithSelector_ShouldMapResults()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);
        var result = config.Where(pair => pair.Key);
        Assert.Contains("A", result);
    }

    [Fact]
    public void FirstOrDefault_WithPredicate_ShouldReturnMatchingElement()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);
        var result = config.FirstOrDefault(pair => (int)pair.Value == 2);
        Assert.Equal("B", result.Key);
    }

    [Fact]
    public void FirstOrDefault_ShouldReturnFirstElement()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        var result = config.FirstOrDefault();
        Assert.Equal("A", result.Key);
    }

    #endregion

    private class TestableConfigurationBase(ILogger logger, string path) : ConfigurationBase(logger, path)
    {
        public override Task SaveToFile() => Task.CompletedTask;
        public override void LoadFromFile() { }
    }
}