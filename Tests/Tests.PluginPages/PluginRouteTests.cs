using PluginPages;

namespace Tests.PluginPages;

public class PluginRouteTests
{
    [Theory]
    [InlineData("/", "/")]
    [InlineData("/settings", "/settings")]
    [InlineData("/settings/", "/settings")]
    [InlineData("/Users/{id:int}", "/Users/{id:int}")]
    [InlineData("/reports/{year?}", "/reports/{year?}")]
    public void TryNormalize_AcceptsValidRoutes(string route, string expected)
    {
        Assert.True(PluginRoute.TryNormalize(route, "_assets", out string normalized, out string error), error);
        Assert.Equal(expected, normalized);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("settings")]
    [InlineData("~/settings")]
    [InlineData("/a?b=1")]
    [InlineData("/a#top")]
    [InlineData("/a\\b")]
    [InlineData("//a")]
    [InlineData("/a//b")]
    [InlineData("/../a")]
    [InlineData("/a/./b")]
    [InlineData("/_assets")]
    [InlineData("/_ASSETS/x")]
    public void TryNormalize_RejectsInvalidRoutes(string? route)
    {
        Assert.False(PluginRoute.TryNormalize(route, "_assets", out _, out string error));
        Assert.False(string.IsNullOrEmpty(error));
    }

    [Theory]
    [InlineData("/_assetsx")]
    [InlineData("/files/_assets")]
    public void TryNormalize_ReservedSegmentOnlyBlocksTheFirstSegment(string route)
    {
        Assert.True(PluginRoute.TryNormalize(route, "_assets", out _, out _));
    }

    [Fact]
    public void NormalizeRequestPath_AddsLeadingAndTrimsTrailingSlash()
    {
        Assert.Equal("/", PluginPageRegistry.NormalizeRequestPath(null));
        Assert.Equal("/", PluginPageRegistry.NormalizeRequestPath(""));
        Assert.Equal("/items/5", PluginPageRegistry.NormalizeRequestPath("items/5/"));
        Assert.Equal("/items/5", PluginPageRegistry.NormalizeRequestPath("/items/5"));
    }
}
