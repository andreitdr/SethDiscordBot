using DiscordBotCore.Utilities;

namespace Tests.DiscordBotCore.Utilities;

public class ResultTests
{
    [Fact]
    public void Success_MatchesTheSuccessBranch()
    {
        Result result = Result.Success();

        Assert.True(result.IsSuccess);
        Assert.Equal("ok", result.Match(() => "ok", ex => ex.Message));

        bool called = false;
        result.Match(() => called = true, _ => Assert.Fail("The error branch ran."));
        Assert.True(called);
    }

    [Fact]
    public void Success_DoesNotCallTheExceptionHandler()
    {
        Result.Success().HandleException(_ => Assert.Fail("The handler ran."));
    }

    [Fact]
    public void FailureWithException_PassesTheSameException()
    {
        var exception = new InvalidOperationException("broken");
        Result result = Result.Failure(exception);

        Assert.False(result.IsSuccess);
        Exception? handled = null;
        result.HandleException(ex => handled = ex);
        Assert.Same(exception, handled);
        Assert.Same(exception, result.Match<Exception?>(() => null, ex => ex));
    }

    [Fact]
    public void FailureWithMessage_WrapsItInAnException()
    {
        Result result = Result.Failure("broken");

        Assert.False(result.IsSuccess);
        Assert.Equal("broken", result.Match(() => "ok", ex => ex.Message));

        string? message = null;
        result.Match(() => Assert.Fail("The success branch ran."), ex => message = ex.Message);
        Assert.Equal("broken", message);
    }

    [Fact]
    public void GenericResult_FromValue_MatchesTheValue()
    {
        Result<string> result = Result<string>.From("value");

        Assert.Equal("value", result.Match(value => value, ex => ex.Message));
    }

    [Fact]
    public void GenericResult_FromException_MatchesTheException()
    {
        Result<string> result = new InvalidOperationException("broken");

        Assert.Equal("broken", result.Match(value => value, ex => ex.Message));
    }

    [Fact]
    public void GenericResult_OfValueType_FromException_MatchesTheException()
    {
        Result<int> result = new InvalidOperationException("broken");

        Assert.Equal("broken", result.Match(value => value.ToString(), ex => ex.Message));

        Exception? caught = null;
        result.Match(_ => Assert.Fail("The value branch ran."), ex => caught = ex);
        Assert.NotNull(caught);
    }

    [Fact]
    public void GenericResult_OfValueType_FromDefaultValue_MatchesTheValue()
    {
        Result<int> result = Result<int>.From(0);

        Assert.Equal("0", result.Match(value => value.ToString(), ex => ex.Message));
    }
}
