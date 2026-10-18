using PluginPages;

namespace Tests.PluginPages;

public class RouteValueConverterTests
{
    private enum Color { Red, Green }

    public static TheoryData<string, Type, object?> Convertible => new()
    {
        { "text", typeof(string), "text" },
        { "text", typeof(object), "text" },
        { "42", typeof(int), 42 },
        { "-42", typeof(int?), -42 },
        { "9223372036854775807", typeof(long), long.MaxValue },
        { "18446744073709551615", typeof(ulong), ulong.MaxValue },
        { "true", typeof(bool), true },
        { "1.5", typeof(decimal), 1.5m },
        { "1.5", typeof(double), 1.5d },
        { "1.5", typeof(float), 1.5f },
        { "green", typeof(Color), Color.Green },
    };

    [Theory]
    [MemberData(nameof(Convertible))]
    public void TryConvert_ConvertsSupportedTypes(string text, Type type, object? expected)
    {
        Assert.True(RouteValueConverter.TryConvert(text, type, out object? value));
        Assert.Equal(expected, value);
    }

    [Fact]
    public void TryConvert_ConvertsGuidsAndDates()
    {
        Guid guid = Guid.NewGuid();
        Assert.True(RouteValueConverter.TryConvert(guid.ToString(), typeof(Guid), out object? parsedGuid));
        Assert.Equal(guid, parsedGuid);

        Assert.True(RouteValueConverter.TryConvert("2026-10-18", typeof(DateTime), out object? parsedDate));
        Assert.Equal(new DateTime(2026, 10, 18), parsedDate);
    }

    [Fact]
    public void TryConvert_UsesTheInvariantCulture()
    {
        var previous = Thread.CurrentThread.CurrentCulture;
        Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
        try
        {
            Assert.True(RouteValueConverter.TryConvert("1.5", typeof(double), out object? value));
            Assert.Equal(1.5d, value);
        }
        finally
        {
            Thread.CurrentThread.CurrentCulture = previous;
        }
    }

    [Theory]
    [InlineData("abc", typeof(int))]
    [InlineData("99999999999", typeof(int))]
    [InlineData("-1", typeof(ulong))]
    [InlineData("yes", typeof(bool))]
    [InlineData("not-a-guid", typeof(Guid))]
    [InlineData("blue", typeof(Color))]
    [InlineData("1", typeof(Uri))]
    public void TryConvert_FailsWithNullForInvalidValues(string text, Type type)
    {
        Assert.False(RouteValueConverter.TryConvert(text, type, out object? value));
        Assert.Null(value);
    }
}
