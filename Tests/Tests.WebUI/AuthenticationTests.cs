using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Tests.WebUI;

/// <summary>Sign-in, sign-out and the no-password mode, through the real HTTP pipeline.</summary>
public partial class AuthenticationTests
{
    private const string Password = "test-password-123";

    private static HttpClient Client(DashboardFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });

    private static async Task<HttpResponseMessage> PostLoginAsync(HttpClient client, string password, string returnUrl = "/")
    {
        string loginUrl = "/login?ReturnUrl=" + Uri.EscapeDataString(returnUrl);
        string form = await client.GetStringAsync(loginUrl);
        string token = AntiforgeryField().Match(form).Groups[1].Value;
        Assert.False(string.IsNullOrEmpty(token), "The sign-in form has no antiforgery token.");

        return await client.PostAsync(loginUrl, new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "login",
            ["__RequestVerificationToken"] = token,
            ["Input.Password"] = password,
        }));
    }

    [GeneratedRegex("name=\"__RequestVerificationToken\" value=\"([^\"]+)\"")]
    private static partial Regex AntiforgeryField();

    #region With a password

    [Fact]
    public async Task Dashboard_RedirectsToTheSignInPage()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location?.AbsolutePath);
        Assert.Contains("ReturnUrl=%2F", response.Headers.Location?.Query);
    }

    [Theory]
    [InlineData("/plugin/abc/settings")]
    [InlineData("/plugin/abc/_assets/x.css")]
    public async Task PluginPagesAndAssets_NeedASignIn(string url)
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/login", response.Headers.Location?.AbsolutePath);
    }

    [Fact]
    public async Task BlazorHub_RefusesAnonymousConnections()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await client.PostAsync("/_blazor/negotiate?negotiateVersion=1", null);

        Assert.True(response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.Redirect, $"Got {response.StatusCode}");
    }

    [Fact]
    public async Task SignInPage_IsPublicAndHasAPasswordField()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await client.GetAsync("/login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        string html = await response.Content.ReadAsStringAsync();
        Assert.Contains("type=\"password\"", html);
        Assert.Contains("autocomplete=\"current-password\"", html);
    }

    [Fact]
    public async Task StaticAssets_ArePublic()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await client.GetAsync("/app.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task WrongPassword_ShowsAnErrorAndDoesNotSignIn()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await PostLoginAsync(client, "wrong");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Wrong password", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task RightPassword_SignsInWithASecureCookie()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await PostLoginAsync(client, Password);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", new Uri(new Uri("http://localhost"), response.Headers.Location!).AbsolutePath);
        string cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("SethDiscordBot.Auth="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task SignIn_ReturnsToTheRequestedLocalPage()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await PostLoginAsync(client, Password, "/plugin/abc/settings");

        Assert.Equal("/plugin/abc/settings", new Uri(new Uri("http://localhost"), response.Headers.Location!).AbsolutePath);
    }

    [Fact]
    public async Task SignIn_NeverRedirectsToAnotherSite()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await PostLoginAsync(client, Password, "//evil.example.com/");

        Uri target = new Uri(new Uri("http://localhost"), response.Headers.Location!);
        Assert.Equal("localhost", target.Host);
        Assert.Equal("/", target.AbsolutePath);
    }

    [Fact]
    public async Task SignIn_WithoutAntiforgeryToken_IsRejected()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);

        HttpResponseMessage response = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "login",
            ["Input.Password"] = Password,
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task SignOut_EndsTheSession()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);
        await PostLoginAsync(client, Password);

        HttpResponseMessage response = await client.PostAsync("/logout", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/login", response.Headers.Location?.OriginalString);
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/")).StatusCode);
    }

    [Fact]
    public async Task ChangingThePassword_SignsEveryoneOut()
    {
        string cookie;
        using (var before = new DashboardFactory(Password))
        using (var client = Client(before))
        {
            HttpResponseMessage response = await PostLoginAsync(client, Password);
            cookie = response.Headers.GetValues("Set-Cookie").Single(c => c.StartsWith("SethDiscordBot.Auth=")).Split(';')[0];
        }

        using var after = new DashboardFactory("a-new-password");
        using var fresh = after.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = false });
        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.Add("Cookie", cookie);

        Assert.Equal(HttpStatusCode.Redirect, (await fresh.SendAsync(request)).StatusCode);
    }

    [Fact]
    public async Task SignIn_IsRateLimited()
    {
        using var factory = new DashboardFactory(Password);
        using var client = Client(factory);
        string form = await client.GetStringAsync("/login");
        string token = AntiforgeryField().Match(form).Groups[1].Value;

        var statuses = new List<HttpStatusCode>();
        for (int i = 0; i < 6; i++)
        {
            HttpResponseMessage response = await client.PostAsync("/login", new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["_handler"] = "login",
                ["__RequestVerificationToken"] = token,
                ["Input.Password"] = "wrong",
            }));
            statuses.Add(response.StatusCode);
        }

        Assert.All(statuses.Take(5), status => Assert.Equal(HttpStatusCode.OK, status));
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
    }

    #endregion

    #region Without a password

    [Fact]
    public async Task WithoutPassword_TheLocalMachineGetsTheDashboard()
    {
        using var factory = new DashboardFactory(null);
        using var client = Client(factory);

        Assert.Equal(HttpStatusCode.OK, (await client.GetAsync("/")).StatusCode);
    }

    [Theory]
    [InlineData("127.0.0.1")]
    [InlineData("::1")]
    [InlineData("::ffff:127.0.0.1")]
    public async Task WithoutPassword_LoopbackAddressesAreAllowed(string address)
    {
        using var factory = new DashboardFactory(null);

        var context = await factory.Server.SendAsync(c =>
        {
            c.Request.Path = "/";
            c.Connection.RemoteIpAddress = IPAddress.Parse(address);
        });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task WithoutPassword_OtherMachinesAreRefused()
    {
        using var factory = new DashboardFactory(null);

        var context = await factory.Server.SendAsync(c =>
        {
            c.Request.Path = "/";
            c.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");
        });

        Assert.Equal(StatusCodes.Status403Forbidden, context.Response.StatusCode);
        string body = await new StreamReader(context.Response.Body).ReadToEndAsync();
        Assert.Contains("Dashboard__Password", body);
    }

    [Fact]
    public async Task WithoutPassword_RemoteAccessCanBeAllowedExplicitly()
    {
        using var factory = new DashboardFactory(null, allowRemote: true);

        var context = await factory.Server.SendAsync(c =>
        {
            c.Request.Path = "/";
            c.Connection.RemoteIpAddress = IPAddress.Parse("192.168.1.20");
        });

        Assert.Equal(StatusCodes.Status200OK, context.Response.StatusCode);
    }

    [Fact]
    public async Task WithoutPassword_TheSignInPageSaysSo()
    {
        using var factory = new DashboardFactory(null);
        using var client = Client(factory);

        string html = await client.GetStringAsync("/login");

        Assert.Contains("No dashboard password is set", html);
        Assert.DoesNotContain("type=\"password\"", html);
    }

    #endregion
}
