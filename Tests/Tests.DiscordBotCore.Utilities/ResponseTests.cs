using DiscordBotCore.Utilities.Responses;

namespace Tests.DiscordBotCore.Utilities;

public class ResponseTests
{
    [Fact]
    public void Success_IsSuccessfulWithoutMessage()
    {
        IResponse<bool> response = Response.Success();

        Assert.True(response.IsSuccess);
        Assert.True(response.Data);
        Assert.Equal(string.Empty, response.Message);
    }

    [Fact]
    public void Failure_CarriesTheMessage()
    {
        IResponse<bool> response = Response.Failure("it broke");

        Assert.False(response.IsSuccess);
        Assert.False(response.Data);
        Assert.Equal("it broke", response.Message);
    }

    [Fact]
    public void GenericSuccess_CarriesTheData()
    {
        var data = new List<int> { 1 };
        IResponse<List<int>> response = Response<List<int>>.Success(data);

        Assert.True(response.IsSuccess);
        Assert.Same(data, response.Data);
        Assert.Equal(string.Empty, response.Message);
    }

    [Fact]
    public void GenericFailure_HasNoData()
    {
        IResponse<string> response = Response<string>.Failure("not found");

        Assert.False(response.IsSuccess);
        Assert.Null(response.Data);
        Assert.Equal("not found", response.Message);
    }

    [Fact]
    public void Responses_AreCovariant()
    {
        IResponse<object> response = Response<string>.Success("value");

        Assert.Equal("value", response.Data);
    }
}
