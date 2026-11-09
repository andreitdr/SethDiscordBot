using System.Net;
using System.Text;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Helpers;
using DiscordBotCore.PluginManagement.Models;
using Moq;

namespace Tests.DiscordBotCore.PluginManagement;

public class RepositoryCompatibilityTests
{
    private const string Base = "https://plugins.example.com/api/v1/";
    private const string Other = "https://other.example.com/api/v1/";

    private readonly FakeHttpHandler _http = new();

    private sealed class MutableConfiguration : IPluginRepositoryConfiguration
    {
        public string BaseUrl { get; set; } = Base;
        public string PluginRepositoryLocation => "plugin/";
        public string DependenciesRepositoryLocation => "dependency/";
        public bool IsConfigured => true;
    }

    private PluginRepository CreateRepository(IPluginRepositoryConfiguration? configuration = null) =>
        new(configuration ?? new MutableConfiguration(), new Mock<ILogger>().Object, new HttpClient(_http));

    private void Server(int protocolVersion, int minimumClientVersion, string baseUrl = Base) =>
        _http.Add(baseUrl + "compatibility", Encoding.UTF8.GetBytes($$"""{"protocolVersion":{{protocolVersion}},"minimumClientProtocolVersion":{{minimumClientVersion}}}"""));

    [Fact]
    public async Task SameProtocol_IsCompatible_AndCatalogueRequestsGoThrough()
    {
        Server(RepositoryProtocol.ClientVersion, RepositoryProtocol.ClientVersion);
        _http.Add(Base + "plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", Encoding.UTF8.GetBytes("[]"));
        PluginRepository repository = CreateRepository();

        Assert.Equal(RepositoryCompatibility.Compatible, await repository.CheckCompatibilityAsync());
        Assert.Empty(await repository.GetAllPlugins(1, false));
        Assert.Contains(Base + "plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", _http.RequestedUrls);
    }

    [Fact]
    public async Task ServerNeedingANewerBot_IsBotTooOld()
    {
        Server(RepositoryProtocol.ClientVersion + 1, RepositoryProtocol.ClientVersion + 1);

        Assert.Equal(RepositoryCompatibility.BotTooOld, await CreateRepository().CheckCompatibilityAsync());
    }

    [Fact]
    public async Task ServerOlderThanTheBotAccepts_IsServerTooOld()
    {
        Server(RepositoryProtocol.MinimumServerVersion - 1, 1);

        Assert.Equal(RepositoryCompatibility.ServerTooOld, await CreateRepository().CheckCompatibilityAsync());
    }

    [Fact]
    public async Task ServerWithoutTheRoute_IsServerTooOld()
    {
        // The fake answers 404 for anything it doesn't know
        Assert.Equal(RepositoryCompatibility.ServerTooOld, await CreateRepository().CheckCompatibilityAsync());
    }

    [Fact]
    public async Task SomethingThatIsntARepository_IsServerTooOld()
    {
        _http.Add(Base + "compatibility", Encoding.UTF8.GetBytes("<html>hello</html>"));

        Assert.Equal(RepositoryCompatibility.ServerTooOld, await CreateRepository().CheckCompatibilityAsync());
    }

    [Theory]
    [InlineData(HttpStatusCode.InternalServerError)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task ServerErrors_AreNotReportedAsVersionProblems(HttpStatusCode status)
    {
        _http.Add(Base + "compatibility", [], status);

        Assert.Equal(RepositoryCompatibility.Unreachable, await CreateRepository().CheckCompatibilityAsync());
    }

    [Fact]
    public async Task ConnectionFailures_AreNotReportedAsVersionProblems()
    {
        var repository = new PluginRepository(new MutableConfiguration(), new Mock<ILogger>().Object, new HttpClient(new FailingHandler()));

        Assert.Equal(RepositoryCompatibility.Unreachable, await repository.CheckCompatibilityAsync());
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new HttpRequestException("Connection refused");
    }

    [Fact]
    public async Task WhenIncompatible_NoCatalogueRequestIsMade()
    {
        Server(RepositoryProtocol.ClientVersion + 1, RepositoryProtocol.ClientVersion + 1);
        PluginRepository repository = CreateRepository();

        Assert.Empty(await repository.GetAllPlugins(1, false));
        Assert.Null(await repository.GetPluginById(1));
        Assert.Null(await repository.GetPluginByName("Plugin", 1, false));
        Assert.Empty(await repository.GetDependenciesForPlugin(1));
        Assert.Null(await repository.GetNuGetDependenciesForPlugin(1));

        Assert.All(_http.RequestedUrls, url => Assert.EndsWith("/compatibility", url));
    }

    [Fact]
    public async Task TheAnswerIsKept_UntilARecheckIsAsked()
    {
        Server(RepositoryProtocol.ClientVersion, 1);
        PluginRepository repository = CreateRepository();

        await repository.CheckCompatibilityAsync();
        await repository.CheckCompatibilityAsync();
        Assert.Single(_http.RequestedUrls);

        await repository.CheckCompatibilityAsync(recheck: true);
        Assert.Equal(2, _http.RequestedUrls.Count);
    }

    [Fact]
    public async Task AChangedBaseUrlIsAskedAgain_AndUsedForRequests()
    {
        Server(RepositoryProtocol.ClientVersion + 1, RepositoryProtocol.ClientVersion + 1);
        Server(RepositoryProtocol.ClientVersion, RepositoryProtocol.ClientVersion, Other);
        _http.Add(Other + "plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", Encoding.UTF8.GetBytes("[]"));
        var configuration = new MutableConfiguration();
        PluginRepository repository = CreateRepository(configuration);
        Assert.Equal(RepositoryCompatibility.BotTooOld, await repository.CheckCompatibilityAsync());

        configuration.BaseUrl = Other;

        Assert.Equal(RepositoryCompatibility.Compatible, await repository.CheckCompatibilityAsync());
        await repository.GetAllPlugins(1, false);
        Assert.Contains(Other + "plugin/get-all-plugins?operatingSystem=1&includeNotApproved=False", _http.RequestedUrls);
    }

    [Fact]
    public async Task EveryRequestSaysItIsTheBotAndWhichProtocolItSpeaks()
    {
        var capture = new HeaderCapture();
        var repository = new PluginRepository(new MutableConfiguration(), new Mock<ILogger>().Object, new HttpClient(capture));

        await repository.CheckCompatibilityAsync();

        Assert.Equal(RepositoryProtocol.ClientVersion.ToString(), capture.Protocol);
        Assert.Equal(RepositoryProtocol.UserAgent, capture.UserAgent);
        Assert.Matches(@"^SethDiscordBot/\S+$", capture.UserAgent);
    }

    private sealed class HeaderCapture : HttpMessageHandler
    {
        public string? Protocol { get; private set; }
        public string? UserAgent { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Protocol = request.Headers.GetValues(RepositoryProtocol.HeaderName).Single();
            UserAgent = request.Headers.UserAgent.ToString();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    [Fact]
    public async Task NuGetRoute_ParsesTheApiResponse()
    {
        Server(RepositoryProtocol.ClientVersion, 1);
        _http.Add(Base + "dependency/get-nuget-by-plugin-id?pluginId=3", Encoding.UTF8.GetBytes("""[{"PackageId":"Humanizer.Core","Version":"3.0.10"}]"""));

        OnlineNuGetDependency package = Assert.Single((await CreateRepository().GetNuGetDependenciesForPlugin(3))!);

        Assert.Equal("Humanizer.Core", package.PackageId);
        Assert.Equal("3.0.10", package.Version);
    }

    [Fact]
    public async Task DependencyRoute_ParsesTheFieldsTheApiSends()
    {
        Server(RepositoryProtocol.ClientVersion, 1);
        _http.Add(Base + "dependency/get-by-plugin-id?pluginId=3", Encoding.UTF8.GetBytes(
            """[{"Id":1,"Name":"tool.exe","DownloadLink":"https://x/1","IsExecutable":true,"DependencyName":"tool.exe","DependencyLink":"https://x/1","DependencyLocation":"bin/tool.exe"}]"""));

        OnlineDependencyInfo dependency = Assert.Single(await CreateRepository().GetDependenciesForPlugin(3));

        Assert.Equal("tool.exe", dependency.DependencyName);
        Assert.Equal("https://x/1", dependency.DownloadLink);
        Assert.Equal("bin/tool.exe", dependency.DownloadLocation);
        Assert.True(dependency.IsExecutable);
    }
}
