using PluginPages;
using Tests.PluginPages.Fixtures;

namespace Tests.PluginPages;

public class PluginPageRegistryTests
{
    private const string Key = "3f2c7c1e-0000-4000-8000-000000000001";

    [Fact]
    public void Register_AcceptsValidPagesAndBuildsHrefsUnderThePluginPrefix()
    {
        var registry = TestRegistry.Create();

        var result = registry.Register(Key, typeof(HomePage), typeof(SettingsPage), typeof(ItemPage));

        Assert.Empty(result.Rejected);
        Assert.Equal(3, result.Accepted.Count);
        Assert.Contains(result.Accepted, p => p.Route == "/" && p.Href == $"/plugin/{Key}");
        Assert.Contains(result.Accepted, p => p.Route == "/settings" && p.Href == $"/plugin/{Key}/settings");
    }

    [Fact]
    public void Register_OnlyTitledParameterlessPagesShowInMenu()
    {
        var registry = TestRegistry.Create();

        registry.Register(Key, typeof(HomePage), typeof(ItemPage), typeof(TwoRoutesPage));

        var menu = registry.Pages.Where(p => p.ShowInMenu).Select(p => p.Route).OrderBy(r => r).ToArray();
        Assert.Equal(["/", "/b"], menu);
    }

    [Fact]
    public void Register_RejectsOverlappingRoutesAndKeepsTheFirstByTypeName()
    {
        var registry = TestRegistry.Create();

        // /items/{number} and /items/{id:int} have the same shape. "ConflictingItemPage" sorts before "ItemPage".
        var result = registry.Register(Key, typeof(ItemPage), typeof(ConflictingItemPage));

        var accepted = Assert.Single(result.Accepted);
        Assert.Equal(typeof(ConflictingItemPage).FullName, accepted.ComponentTypeName);
        var rejected = Assert.Single(result.Rejected);
        Assert.Equal(typeof(ItemPage).FullName, rejected.ComponentTypeName);
        Assert.Contains("overlaps", rejected.Reason);
        Assert.Single(registry.Rejections);
    }

    [Fact]
    public void Register_TreatsOptionalParametersAsBothShapes()
    {
        var registry = TestRegistry.Create();

        var result = registry.Register(Key, typeof(ReportsPage), typeof(ReportsIndexPage));

        Assert.Equal(typeof(ReportsIndexPage).FullName, Assert.Single(result.Accepted).ComponentTypeName);
        Assert.Contains("overlaps", Assert.Single(result.Rejected).Reason);
    }

    [Fact]
    public void Register_AllowsLiteralsNextToParametersAndCatchAlls()
    {
        var registry = TestRegistry.Create();

        var result = registry.Register(Key, typeof(NewItemPage), typeof(ItemPage), typeof(FilesPage), typeof(ReadmePage));

        Assert.Empty(result.Rejected);
        Assert.Equal(4, result.Accepted.Count);
    }

    [Theory]
    [InlineData(typeof(UnknownConstraintPage), "unknown")]
    [InlineData(typeof(ReservedSegmentPage), "reserved")]
    [InlineData(typeof(NotAComponent), "IComponent")]
    [InlineData(typeof(AbstractPage), "non-abstract")]
    [InlineData(typeof(NoDefaultConstructorPage), "parameterless")]
    [InlineData(typeof(InternalPage), "public")]
    public void Register_RejectsInvalidPagesWithAReason(Type pageType, string reasonFragment)
    {
        var registry = TestRegistry.Create();

        var result = registry.Register(Key, pageType);

        Assert.Empty(result.Accepted);
        var rejection = Assert.Single(result.Rejected);
        Assert.Contains(reasonFragment, rejection.Reason, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("")]
    [InlineData("has space")]
    [InlineData("a/b")]
    public void Register_RejectsInvalidKeys(string key)
    {
        var registry = TestRegistry.Create();

        var result = registry.Register(key, typeof(HomePage));

        Assert.Empty(result.Accepted);
        Assert.Single(result.Rejected);
        Assert.Empty(registry.Pages);
    }

    [Theory]
    [InlineData("items/new", typeof(NewItemPage))]
    [InlineData("items/5", typeof(ItemPage))]
    [InlineData("files/readme", typeof(ReadmePage))]
    [InlineData("files/a/b/c.txt", typeof(FilesPage))]
    [InlineData(null, typeof(HomePage))]
    [InlineData("", typeof(HomePage))]
    [InlineData("SETTINGS", typeof(SettingsPage))]
    [InlineData("settings/", typeof(SettingsPage))]
    public void TryMatch_PrefersTheMostSpecificRoute(string? path, Type expected)
    {
        var registry = TestRegistry.Create();
        registry.Register(Key, typeof(HomePage), typeof(SettingsPage), typeof(NewItemPage), typeof(ItemPage), typeof(FilesPage), typeof(ReadmePage));

        Assert.True(registry.TryMatch(Key, path, out var match));
        Assert.Equal(expected, match.ComponentType);
    }

    [Theory]
    [InlineData("items/abc")]
    [InlineData("items/5/extra")]
    [InlineData("missing")]
    public void TryMatch_FailsWhenNoRouteOrConstraintMatches(string path)
    {
        var registry = TestRegistry.Create();
        registry.Register(Key, typeof(HomePage), typeof(ItemPage));

        Assert.False(registry.TryMatch(Key, path, out _));
    }

    [Fact]
    public void TryMatch_ConvertsRouteValuesToComponentParameters()
    {
        var registry = TestRegistry.Create();
        var id = Guid.NewGuid();
        registry.Register(Key, typeof(ItemPage), typeof(UserPage), typeof(FilesPage), typeof(ReportsPage));

        Assert.True(registry.TryMatch(Key, "items/42", out var item));
        Assert.Equal(42, item.Parameters["Id"]);
        Assert.Equal("42", item.RouteValues["id"]);

        Assert.True(registry.TryMatch(Key, $"users/{id}", out var user));
        Assert.Equal(id, user.Parameters["Id"]);

        Assert.True(registry.TryMatch(Key, "files/docs/a.txt", out var file));
        Assert.Equal("docs/a.txt", file.Parameters["Path"]);

        Assert.True(registry.TryMatch(Key, "reports", out var reports));
        Assert.False(reports.Parameters.ContainsKey("Year"));
        Assert.True(registry.TryMatch(Key, "reports/2026", out var report));
        Assert.Equal(2026, report.Parameters["Year"]);
    }

    [Fact]
    public void TryMatch_ProvidesAPageContext()
    {
        var registry = TestRegistry.Create();
        registry.Register(Key, typeof(SettingsPage));

        Assert.True(registry.TryMatch(Key, "settings", out var match));

        Assert.Equal(Key, match.Context.PluginKey);
        Assert.Equal($"/plugin/{Key}", match.Context.BasePath);
        Assert.Equal("/settings", match.Context.RelativePath);
        Assert.Equal("Settings", match.Context.Title);
        Assert.Equal($"/plugin/{Key}/items/2", match.Context.Href("items/2"));
        Assert.Equal($"/plugin/{Key}", match.Context.Href());
        Assert.StartsWith($"/plugin/{Key}/_assets/site.css?v=", match.Context.AssetUrl("/site.css"));
    }

    [Fact]
    public void KeysAreIsolatedAndCaseInsensitive()
    {
        var registry = TestRegistry.Create();
        registry.Register("one", typeof(HomePage));
        registry.Register("two", typeof(HomePage));

        Assert.True(registry.TryMatch("ONE", "/", out var one));
        Assert.Equal("one", one.Context.PluginKey);
        Assert.Equal(2, registry.Pages.Count);
        Assert.False(registry.TryMatch("three", "/", out _));
    }

    [Fact]
    public void RegisterAgain_ReplacesThePreviousRegistration()
    {
        var registry = TestRegistry.Create();
        registry.Register(Key, typeof(HomePage), typeof(SettingsPage));

        registry.Register(Key, typeof(HomePage));

        Assert.Single(registry.Pages);
        Assert.False(registry.TryMatch(Key, "settings", out _));
    }

    [Fact]
    public void UnregisterAndClear_RemovePagesAndRaiseChanged()
    {
        var registry = TestRegistry.Create();
        int changes = 0;
        registry.Changed += () => changes++;

        registry.Register("one", typeof(HomePage));
        registry.Register("two", typeof(HomePage));
        Assert.Equal(2, changes);

        Assert.True(registry.Unregister("one"));
        Assert.False(registry.Unregister("one"));
        Assert.Equal(3, changes);
        Assert.False(registry.IsRegistered("one"));

        registry.Clear();
        Assert.Equal(4, changes);
        Assert.Empty(registry.Pages);

        registry.Clear();
        Assert.Equal(4, changes);
    }

    [Fact]
    public void AFailingChangedHandlerDoesNotStopOthers()
    {
        var registry = TestRegistry.Create();
        bool secondCalled = false;
        registry.Changed += () => throw new InvalidOperationException();
        registry.Changed += () => secondCalled = true;

        registry.Register(Key, typeof(HomePage));

        Assert.True(secondCalled);
    }

    [Fact]
    public void Register_WithoutAnyPagesRegistersNothing()
    {
        var registry = TestRegistry.Create();

        var result = registry.Register(Key, typeof(PluginPageRegistryTests));

        Assert.Empty(result.Accepted);
        Assert.Empty(result.Rejected);
        Assert.False(registry.IsRegistered(Key));
    }
}
