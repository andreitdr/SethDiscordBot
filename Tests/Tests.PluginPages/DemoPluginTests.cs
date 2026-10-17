using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using PluginPages;

namespace Tests.PluginPages;

/// <summary>Loads the real WebPagesDemo plugin into a collectible context, as the bot's plugin loader does.</summary>
public class DemoPluginTests
{
    private const string Key = "demo";
    private static readonly string DemoPath = Path.Combine(AppContext.BaseDirectory, "TestPlugins", "WebPagesDemo.dll");

    [Fact]
    public async Task DemoPlugin_RegistersItsPagesAndServesEmbeddedAssets()
    {
        var context = new AssemblyLoadContext("demo-pages", isCollectible: true);
        try
        {
            var registry = TestRegistry.Create();
            var result = registry.Register(Key, "Web pages demo", context.LoadFromAssemblyPath(DemoPath));

            Assert.Empty(result.Rejected);
            Assert.Equal(["/", "/about", "/items/{id:int}"], result.Accepted.Select(p => p.Route).OrderBy(r => r, StringComparer.Ordinal));
            Assert.True(registry.TryMatch(Key, "items/7", out var item));
            Assert.Equal(7, item.Parameters["Id"]);

            Assert.True(registry.TryOpenAsset(Key, "demo.css", out Stream css));
            using (var reader = new StreamReader(css))
            {
                Assert.Contains(".demo-page", await reader.ReadToEndAsync());
            }

            Assert.False(registry.TryOpenAsset(Key, "../demo.css", out _));
            Assert.False(registry.TryOpenAsset(Key, "missing.css", out _));
        }
        finally
        {
            context.Unload();
        }
    }

    [Fact]
    public async Task DemoPlugin_RendersThroughTheOutlet()
    {
        var context = new AssemblyLoadContext("demo-render", isCollectible: true);
        try
        {
            await using var services = TestRegistry.CreateServices(withRazorComponents: true);
            var registry = services.GetRequiredService<IPluginPageRegistry>();
            registry.Register(Key, "Web pages demo", context.LoadFromAssemblyPath(DemoPath));

            string html = await RenderOutletAsync(services, "items/42");

            Assert.Contains("Item 42", html);
            Assert.Contains("/plugin/demo/items/43", html);
            Assert.Contains("no page at this address", await RenderOutletAsync(services, "items/abc"));

            registry.Clear();
            Assert.Contains("not loaded", await RenderOutletAsync(services, "items/42"));
        }
        finally
        {
            context.Unload();
        }
    }

    /// <summary>
    /// The plugin's load context must be collectable after its pages were rendered and unregistered; otherwise every
    /// plugin reload would leak the previous copy.
    /// </summary>
    [Fact]
    public async Task DemoPlugin_LoadContextIsCollectedAfterUnregister()
    {
        WeakReference contextReference = await LoadRenderAndUnloadAsync();

        for (int i = 0; i < 20 && contextReference.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            await Task.Delay(10);
        }

        Assert.False(contextReference.IsAlive, "The plugin load context was not collected; something still references a plugin type.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static async Task<WeakReference> LoadRenderAndUnloadAsync()
    {
        var context = new AssemblyLoadContext("demo-unload", isCollectible: true);
        await using (var services = TestRegistry.CreateServices(withRazorComponents: true))
        {
            var registry = services.GetRequiredService<IPluginPageRegistry>();
            registry.Register(Key, "Web pages demo", context.LoadFromAssemblyPath(DemoPath));

            Assert.Contains("Item 3", await RenderOutletAsync(services, "items/3"));
            Assert.Contains("About", await RenderOutletAsync(services, "about"));

            registry.Unregister(Key);
        }

        context.Unload();
        return new WeakReference(context);
    }

    private static async Task<string> RenderOutletAsync(IServiceProvider services, string path)
    {
        await using var renderer = new HtmlRenderer(services, NullLoggerFactory.Instance);
        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(PluginPageOutlet.PluginKey)] = Key,
                [nameof(PluginPageOutlet.PluginPath)] = path
            });
            var output = await renderer.RenderComponentAsync<PluginPageOutlet>(parameters);
            return output.ToHtmlString();
        });
    }
}
