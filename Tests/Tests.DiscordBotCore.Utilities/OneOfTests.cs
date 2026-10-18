using DiscordBotCore.Utilities;

namespace Tests.DiscordBotCore.Utilities;

public class OneOfTests
{
    [Fact]
    public void TwoTypes_MatchesTheFirst()
    {
        OneOf<string, Exception> value = "text";

        Assert.Equal("text", value.Value);
        Assert.Equal(typeof(string), value.GetActualType());
        Assert.Equal("first:text", value.Match(s => "first:" + s, ex => "second"));
    }

    [Fact]
    public void TwoTypes_MatchesTheSecond()
    {
        var exception = new InvalidOperationException();
        OneOf<string, Exception> value = exception;

        Assert.Same(exception, value.Value);
        Assert.Equal(typeof(InvalidOperationException), value.GetActualType());

        object? matched = null;
        value.Match(_ => Assert.Fail("The first branch ran."), ex => matched = ex);
        Assert.Same(exception, matched);
    }

    [Fact]
    public void TwoTypes_WithValueTypes_MatchesTheOneThatWasSet()
    {
        OneOf<int, string> number = 0;
        OneOf<int, string> text = "zero";

        Assert.Equal("int", number.Match(_ => "int", _ => "string"));
        Assert.Equal("string", text.Match(_ => "int", _ => "string"));
        Assert.Equal("zero", text.Value);
        Assert.Equal(typeof(string), text.GetActualType());
    }

    [Fact]
    public void ThreeTypes_MatchesEachBranch()
    {
        OneOf<int, string, bool> first = 1;
        OneOf<int, string, bool> second = "two";
        OneOf<int, string, bool> third = false;

        Assert.Equal("1", Describe(first));
        Assert.Equal("two", Describe(second));
        Assert.Equal("False", Describe(third));

        static string Describe(OneOf<int, string, bool> value) =>
            value.Match(i => i.ToString(), s => s, b => b.ToString());
    }

    [Fact]
    public void ThreeTypes_ActionMatch_RunsOneBranch()
    {
        OneOf<int, string, bool> value = "two";
        var calls = new List<string>();

        value.Match(_ => calls.Add("int"), _ => calls.Add("string"), _ => calls.Add("bool"));

        Assert.Equal(["string"], calls);
    }

    [Fact]
    public void FourTypes_MatchesEachBranch()
    {
        Assert.Equal("a", Describe(1));
        Assert.Equal("b", Describe("x"));
        Assert.Equal("c", Describe(true));
        Assert.Equal("d", Describe(2.5));

        static string Describe(OneOf<int, string, bool, double> value)
        {
            var calls = new List<string>();
            value.Match(_ => calls.Add("a"), _ => calls.Add("b"), _ => calls.Add("c"), _ => calls.Add("d"));
            string matched = value.Match(_ => "a", _ => "b", _ => "c", _ => "d");
            Assert.Equal([matched], calls);
            return matched;
        }
    }
}
