using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using Moq;

namespace Tests.DiscordBotCore.Configuration;

public class ConfigurationTests : IDisposable
{
    private readonly Mock<ILogger> _loggerMock;
    private readonly string _testFilePath;

    public ConfigurationTests()
    {
        _loggerMock = new Mock<ILogger>();
        _testFilePath = Path.GetTempFileName();
    }

    public void Dispose()
    {
        File.Delete(_testFilePath);
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
    public void Add_ShouldNotOverrideAnExistingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("Key1", 1);
        config.Add("Key1", 2);
        Assert.Equal(1, config.Get("Key1"));
    }

    [Fact]
    public void Set_ShouldAddAMissingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Set("Key1", "value");
        Assert.Equal("value", config.Get("Key1"));
    }

    [Fact]
    public void Remove_ShouldIgnoreAMissingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Remove("Missing");
        Assert.False(config.ContainsKey("Missing"));
    }

    [Fact]
    public void Get_WithoutDefault_ShouldReturnDefaultForMissingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        Assert.Null(config.Get<string>("Missing"));
        Assert.Equal(0, config.Get<int>("Missing"));
    }

    [Fact]
    public void Get_ShouldThrowForMissingKeyWithoutType()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        Assert.Throws<KeyNotFoundException>(() => config.Get("Missing"));
    }

    [Fact]
    public void Get_ShouldReturnAValueThatIsAlreadyOfTheRequestedType()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        var list = new List<ulong> { 1, 2 };
        config.Set("List", list);
        Assert.Same(list, config.Get<List<ulong>>("List"));
    }

    [Theory]
    [InlineData("true", true)]
    [InlineData("False", false)]
    public void Get_ShouldConvertStringsToBooleans(string stored, bool expected)
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Set("Flag", stored);
        Assert.Equal(expected, config.Get("Flag", !expected));
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
    public void GetList_ShouldConvertEachItem()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Set("ServerIds", new List<object> { 1234567890123456789L, "42" });
        Assert.Equal(new List<ulong> { 1234567890123456789UL, 42UL }, config.GetList("ServerIds", new List<ulong>()));
    }

    [Fact]
    public void GetList_ShouldThrowIfNotList()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Set("NotList", 123);
        Assert.Throws<Exception>(() => config.GetList("NotList", new List<int>()));
    }

    [Fact]
    public void GetDictionary_ShouldReturnEmptyForMissingKey()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        Assert.Empty(config.GetDictionary<string, string>("Missing"));
    }

    [Fact]
    public void GetDictionary_ShouldConvertKeysAndValues()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Set("Dict", new Dictionary<string, object> { { "1", "10" }, { "2", 20 } });
        var result = config.GetDictionary<int, int>("Dict");
        Assert.Equal(10, result[1]);
        Assert.Equal(20, result[2]);
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
    public void Where_WithIndexedPredicate_ShouldPassTheIndex()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);
        var result = config.Where((pair, index) => index == 1).ToList();
        Assert.Equal("B", Assert.Single(result).Key);
    }

    [Fact]
    public void Where_WithIndexedSelector_ShouldPassTheIndex()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);
        Assert.Equal(["0:A", "1:B"], config.Where((pair, index) => $"{index}:{pair.Key}"));
    }

    [Fact]
    public void FirstOrDefault_ShouldReturnDefaultWhenEmpty()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        Assert.Null(config.FirstOrDefault().Key);
        Assert.Null(config.FirstOrDefault(pair => pair.Key == "X").Key);
    }

    [Fact]
    public void GetEnumerator_ShouldEnumerateEveryPair()
    {
        var config = new TestableConfigurationBase(_loggerMock.Object, _testFilePath);
        config.Add("A", 1);
        config.Add("B", 2);

        var keys = new List<string>();
        using var enumerator = config.GetEnumerator();
        while (enumerator.MoveNext())
        {
            keys.Add(enumerator.Current.Key);
        }

        Assert.Equal(["A", "B"], keys);
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