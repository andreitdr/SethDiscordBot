using DiscordBotCore.Utilities;

namespace Tests.DiscordBotCore.Utilities;

public class OptionTests
{
    [Fact]
    public void Option2_MatchesTheBranchThatWasSet()
    {
        Assert.Equal("int:0", Describe(0));
        Assert.Equal("string:", Describe(""));
        Assert.Equal("error:bad", Describe(new ArgumentException("bad")));

        static string Describe(Option2<int, string, ArgumentException> option)
        {
            string? fromAction = null;
            option.Match(i => fromAction = $"int:{i}", s => fromAction = $"string:{s}", e => fromAction = $"error:{e.Message}");
            string fromFunc = option.Match(i => $"int:{i}", s => $"string:{s}", e => $"error:{e.Message}");
            Assert.Equal(fromFunc, fromAction);
            return fromFunc;
        }
    }

    [Fact]
    public void Option2_ToString_NamesTheType()
    {
        Assert.Equal("Option2<Int32>: 5", new Option2<int, string, Exception>(5).ToString());
        Assert.Equal("Option2<String>: x", new Option2<int, string, Exception>("x").ToString());
        Assert.StartsWith("Option2<Exception>: ", new Option2<int, string, Exception>(new Exception("e")).ToString());
    }

    [Fact]
    public void Option3_MatchesTheBranchThatWasSet()
    {
        Assert.Equal("a", Describe(1));
        Assert.Equal("b", Describe("x"));
        Assert.Equal("c", Describe(false));
        Assert.Equal("error", Describe(new InvalidOperationException()));

        static string Describe(Option3<int, string, bool, InvalidOperationException> option)
        {
            string? fromAction = null;
            option.Match(_ => fromAction = "a", _ => fromAction = "b", _ => fromAction = "c", _ => fromAction = "error");
            string fromFunc = option.Match(_ => "a", _ => "b", _ => "c", _ => "error");
            Assert.Equal(fromFunc, fromAction);
            return fromFunc;
        }
    }

    [Fact]
    public void Option3_ToString_NamesTheType()
    {
        Assert.Equal("Option3<Boolean>: True", new Option3<int, string, bool, Exception>(true).ToString());
    }

    [Fact]
    public void Option4_MatchesTheBranchThatWasSet()
    {
        Assert.Equal("a", Describe(1));
        Assert.Equal("b", Describe("x"));
        Assert.Equal("c", Describe(false));
        Assert.Equal("d", Describe(1.5));
        Assert.Equal("error", Describe(new InvalidOperationException()));

        static string Describe(Option4<int, string, bool, double, InvalidOperationException> option)
        {
            string? fromAction = null;
            option.Match(_ => fromAction = "a", _ => fromAction = "b", _ => fromAction = "c", _ => fromAction = "d", _ => fromAction = "error");
            string fromFunc = option.Match(_ => "a", _ => "b", _ => "c", _ => "d", _ => "error");
            Assert.Equal(fromFunc, fromAction);
            return fromFunc;
        }
    }

    [Fact]
    public void Option4_ToString_NamesTheType()
    {
        Assert.Equal("Option4<Double>: 1.5", new Option4<int, string, bool, double, Exception>(1.5).ToString().Replace(',', '.'));
    }
}
