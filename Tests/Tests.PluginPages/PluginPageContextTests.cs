using PluginPages;

namespace Tests.PluginPages;

public class PluginPageContextTests
{
    private static PluginPageContext Create(string basePath = "/plugin/abc/") =>
        new("abc", "My plugin", basePath, "_assets", "v123", "items/7", "Items", new Dictionary<string, string?> { ["id"] = "7" });

    [Fact]
    public void Constructor_TrimsTheBasePathAndBuildsTheAssetsPath()
    {
        PluginPageContext context = Create();

        Assert.Equal("/plugin/abc", context.BasePath);
        Assert.Equal("/plugin/abc/_assets", context.AssetsPath);
        Assert.Equal("abc", context.PluginKey);
        Assert.Equal("My plugin", context.PluginName);
        Assert.Equal("items/7", context.RelativePath);
        Assert.Equal("Items", context.Title);
        Assert.Equal("7", context.RouteValues["id"]);
    }

    [Theory]
    [InlineData("/", "/plugin/abc")]
    [InlineData("", "/plugin/abc")]
    [InlineData("settings", "/plugin/abc/settings")]
    [InlineData("/items/7", "/plugin/abc/items/7")]
    [InlineData("//items", "/plugin/abc/items")]
    public void Href_IsRelativeToThePlugin(string relative, string expected)
    {
        Assert.Equal(expected, Create().Href(relative));
    }

    [Fact]
    public void Href_WithoutArgument_IsTheIndexPage()
    {
        Assert.Equal("/plugin/abc", Create().Href());
    }

    [Theory]
    [InlineData("demo.css", "/plugin/abc/_assets/demo.css?v=v123")]
    [InlineData("/js/app.js", "/plugin/abc/_assets/js/app.js?v=v123")]
    public void AssetUrl_IsVersioned(string path, string expected)
    {
        Assert.Equal(expected, Create().AssetUrl(path));
    }
}
