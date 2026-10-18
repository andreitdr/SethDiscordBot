using System.Net;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using PluginPages;

namespace Tests.PluginPages;

/// <summary>The <c>/plugin/{key}/_assets/{path}</c> endpoint, serving the demo plugin's embedded files over HTTP.</summary>
public class AssetEndpointTests : IAsyncLifetime
{
    private static readonly string DemoPath = Path.Combine(AppContext.BaseDirectory, "TestPlugins", "WebPagesDemo.dll");

    private readonly AssemblyLoadContext _context = new("demo-assets", isCollectible: true);
    private WebApplication _app = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddPluginPages();

        _app = builder.Build();
        _app.MapPluginPageAssets();
        await _app.StartAsync();

        _app.Services.GetRequiredService<IPluginPageRegistry>().Register("demo", "Web pages demo", _context.LoadFromAssemblyPath(DemoPath));
        _client = _app.GetTestClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _context.Unload();
    }

    [Fact]
    public async Task Asset_IsServedWithItsContentTypeAndNoSniff()
    {
        HttpResponseMessage response = await _client.GetAsync("/plugin/demo/_assets/demo.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("text/css", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").Single());
        Assert.Equal("no-cache", response.Headers.CacheControl?.ToString());
        Assert.Contains(".demo-page", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task VersionedAsset_CanBeCached()
    {
        HttpResponseMessage response = await _client.GetAsync("/plugin/demo/_assets/demo.css?v=abc");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.Private);
        Assert.Equal(TimeSpan.FromDays(1), response.Headers.CacheControl?.MaxAge);
    }

    [Theory]
    [InlineData("/plugin/demo/_assets/missing.css")]
    [InlineData("/plugin/demo/_assets/")]
    [InlineData("/plugin/demo/_assets/..%2Fdemo.css")]
    [InlineData("/plugin/other/_assets/demo.css")]
    public async Task UnknownOrEscapingAsset_IsNotFound(string url)
    {
        HttpResponseMessage response = await _client.GetAsync(url);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UnregisteredPlugin_HasNoAssets()
    {
        _app.Services.GetRequiredService<IPluginPageRegistry>().Unregister("demo");

        HttpResponseMessage response = await _client.GetAsync("/plugin/demo/_assets/demo.css");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
