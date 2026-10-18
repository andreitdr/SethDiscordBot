using Microsoft.Extensions.Configuration;
using WebUI;
using WebUI.Services;

namespace Tests.WebUI;

public class DashboardAccessTests
{
    private static DashboardAccess Create(string? password, string? allowRemote = null) =>
        new(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            [DashboardAccess.PasswordConfigurationKey] = password,
            [DashboardAccess.AllowRemoteWithoutPasswordConfigurationKey] = allowRemote
        }).Build());

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void WithoutPassword_NothingVerifies(string? password)
    {
        DashboardAccess access = Create(password);

        Assert.False(access.IsPasswordRequired);
        Assert.Null(access.PasswordStamp);
        Assert.False(access.Verify(""));
        Assert.False(access.Verify(null));
    }

    [Fact]
    public void WithPassword_OnlyTheExactPasswordVerifies()
    {
        DashboardAccess access = Create("correct horse");

        Assert.True(access.IsPasswordRequired);
        Assert.True(access.Verify("correct horse"));
        Assert.False(access.Verify("Correct horse"));
        Assert.False(access.Verify("correct horse "));
        Assert.False(access.Verify(""));
        Assert.False(access.Verify(null));
    }

    [Fact]
    public void PasswordStamp_ChangesWithThePasswordAndDoesNotRevealIt()
    {
        string? first = Create("one").PasswordStamp;
        string? again = Create("one").PasswordStamp;
        string? other = Create("two").PasswordStamp;

        Assert.NotNull(first);
        Assert.Equal(first, again);
        Assert.NotEqual(first, other);
        Assert.DoesNotContain("one", first);
        Assert.Equal(16, first.Length);
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("false", false)]
    [InlineData("true", true)]
    public void AllowRemoteWithoutPassword_IsReadFromConfiguration(string? value, bool expected)
    {
        Assert.Equal(expected, Create(null, value).AllowRemoteWithoutPassword);
    }

    [Theory]
    [InlineData("/", true)]
    [InlineData("/plugin/abc/settings", true)]
    [InlineData("/?x=1", true)]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("//evil.example.com", false)]
    [InlineData("/\\evil.example.com", false)]
    [InlineData("https://evil.example.com/", false)]
    [InlineData("plugin", false)]
    public void IsLocalUrl_OnlyAcceptsPathsInsideTheApp(string? url, bool expected)
    {
        Assert.Equal(expected, DashboardAuthentication.IsLocalUrl(url));
    }
}
