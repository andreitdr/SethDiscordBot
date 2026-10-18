using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using Microsoft.JSInterop.Infrastructure;
using Moq;
using WebUI.Components.Dashboard;
using WebUI.Services;

namespace Tests.WebUI;

/// <summary>The per-browser panel layout, with the gridstack JS module replaced by a recording fake.</summary>
public class DashboardLayoutServiceTests
{
    private readonly Mock<IJSRuntime> _js = new();
    private readonly Mock<IJSObjectReference> _module = new();
    private readonly List<(string Identifier, object?[]? Args)> _calls = new();
    private string? _savedLayout;

    public DashboardLayoutServiceTests()
    {
        _js.Setup(j => j.InvokeAsync<IJSObjectReference>("import", It.IsAny<object?[]?>()))
           .Returns(new ValueTask<IJSObjectReference>(_module.Object));
        _module.Setup(m => m.InvokeAsync<string?>("loadLayout", It.IsAny<object?[]?>()))
               .Returns(() => new ValueTask<string?>(_savedLayout));
        _module.Setup(m => m.InvokeAsync<IJSVoidResult>(It.IsAny<string>(), It.IsAny<object?[]?>()))
               .Callback<string, object?[]?>((identifier, args) =>
               {
                   _calls.Add((identifier, args));
                   if (identifier == "saveLayout")
                   {
                       _savedLayout = (string)args![1]!;
                   }
               })
               .Returns(new ValueTask<IJSVoidResult>(Mock.Of<IJSVoidResult>()));
    }

    private async Task<DashboardLayoutService> CreateInitializedAsync()
    {
        var layout = new DashboardLayoutService(_js.Object);
        await layout.InitializeAsync(default(ElementReference));
        return layout;
    }

    private IEnumerable<string> Called => _calls.Select(c => c.Identifier);

    private JsonElement Saved => JsonDocument.Parse(_savedLayout!).RootElement;

    [Fact]
    public void BeforeInitializing_EveryPanelHasItsDefaults()
    {
        var layout = new DashboardLayoutService(_js.Object);

        Assert.False(layout.IsLoaded);
        Assert.False(layout.Locked);
        Assert.Equal(PanelRegistry.All.Select(p => p.Id), layout.VisiblePanels.Select(p => p.Id));
        PanelDefinition console = PanelRegistry.Find("console")!;
        Assert.Equal((console.X, console.Y, console.W, console.H), (layout["console"].X, layout["console"].Y, layout["console"].W, layout["console"].H));
    }

    [Fact]
    public async Task Initialize_WithoutASavedLayout_KeepsTheDefaultsAndStartsTheGridAfterRender()
    {
        var layout = await CreateInitializedAsync();

        Assert.True(layout.IsLoaded);
        await layout.AfterRenderAsync();
        Assert.Equal(["init"], Called);

        await layout.AfterRenderAsync();
        Assert.Equal(["init"], Called);
    }

    [Fact]
    public async Task Initialize_AppliesTheSavedLayout()
    {
        _savedLayout = """
            {
              "locked": true,
              "panels": {
                "console": { "x": 1, "y": 2, "w": 5, "h": 7, "expandedH": 7, "visible": true, "collapsed": false },
                "marketplace": { "x": 0, "y": 0, "w": 4, "h": 3, "expandedH": 3, "visible": false, "collapsed": false },
                "removed-panel": { "x": 0, "y": 0, "w": 4, "h": 3, "visible": true }
              }
            }
            """;

        var layout = await CreateInitializedAsync();

        Assert.True(layout.Locked);
        Assert.Equal((1, 2, 5, 7), (layout["console"].X, layout["console"].Y, layout["console"].W, layout["console"].H));
        Assert.DoesNotContain(layout.VisiblePanels, p => p.Id == "marketplace");
        Assert.Equal(PanelRegistry.All.Count - 1, layout.VisiblePanels.Count());
    }

    [Fact]
    public async Task Initialize_EnforcesMinimumSizes()
    {
        _savedLayout = """{ "locked": false, "panels": { "console": { "x": 0, "y": 0, "w": 1, "h": 1, "visible": true } } }""";

        var layout = await CreateInitializedAsync();

        PanelDefinition console = PanelRegistry.Find("console")!;
        Assert.Equal(console.MinW, layout["console"].W);
        Assert.Equal(console.MinH, layout["console"].H);
    }

    [Theory]
    [InlineData("not json")]
    [InlineData("{ \"panels\": 5 }")]
    [InlineData("{}")]
    public async Task Initialize_WithACorruptLayout_FallsBackToTheDefaults(string saved)
    {
        _savedLayout = saved;

        var layout = await CreateInitializedAsync();

        Assert.True(layout.IsLoaded);
        Assert.Equal(PanelRegistry.All.Count, layout.VisiblePanels.Count());
    }

    [Fact]
    public async Task Hide_ReleasesThePanelFromTheGridBeforeRemovingIt()
    {
        var layout = await CreateInitializedAsync();
        int changes = 0;
        layout.Changed += () => changes++;

        await layout.HideAsync("console");

        Assert.Equal(["releaseWidget", "saveLayout"], Called);
        Assert.Equal("console", _calls[0].Args![0]);
        Assert.False(layout["console"].Visible);
        Assert.False(Saved.GetProperty("panels").GetProperty("console").GetProperty("visible").GetBoolean());
        Assert.Equal(1, changes);

        await layout.HideAsync("console");
        Assert.Equal(2, _calls.Count);
    }

    [Fact]
    public async Task Show_AdoptsThePanelAfterTheNextRender()
    {
        var layout = await CreateInitializedAsync();
        await layout.AfterRenderAsync();
        await layout.HideAsync("console");
        _calls.Clear();

        await layout.ShowAsync("console");
        Assert.True(layout["console"].Visible);
        Assert.True(layout["console"].AutoPosition);
        Assert.Equal(["saveLayout"], Called);

        await layout.AfterRenderAsync();
        Assert.Equal(["saveLayout", "adoptWidget"], Called);
        Assert.Equal("console", _calls[1].Args![0]);
    }

    [Fact]
    public async Task ToggleVisibility_HidesAndShows()
    {
        var layout = await CreateInitializedAsync();

        await layout.ToggleVisibilityAsync("stats");
        Assert.False(layout["stats"].Visible);

        await layout.ToggleVisibilityAsync("stats");
        Assert.True(layout["stats"].Visible);
    }

    [Fact]
    public async Task ToggleCollapsed_ShrinksAndRestoresTheHeight()
    {
        var layout = await CreateInitializedAsync();
        int height = layout["console"].H;

        await layout.ToggleCollapsedAsync("console");
        Assert.True(layout["console"].Collapsed);
        Assert.Equal(1, layout["console"].H);
        Assert.Equal(height, layout["console"].ExpandedH);
        Assert.Contains("updateWidget", Called);

        await layout.ToggleCollapsedAsync("console");
        Assert.False(layout["console"].Collapsed);
        Assert.Equal(height, layout["console"].H);
    }

    [Fact]
    public async Task SetLocked_TellsTheGridAndSaves()
    {
        var layout = await CreateInitializedAsync();

        await layout.SetLockedAsync(true);

        Assert.True(layout.Locked);
        Assert.Equal(["setLocked", "saveLayout"], Called);
        Assert.True(Saved.GetProperty("locked").GetBoolean());
    }

    [Fact]
    public async Task OnLayoutChanged_SavesTheNewPositions()
    {
        var layout = await CreateInitializedAsync();

        await layout.OnLayoutChanged([new PanelPosition("console", 0, 3, 8, 6), new PanelPosition("unknown", 1, 1, 1, 1)]);

        Assert.Equal((0, 3, 8, 6), (layout["console"].X, layout["console"].Y, layout["console"].W, layout["console"].H));
        Assert.False(layout["console"].AutoPosition);
        Assert.Equal(8, Saved.GetProperty("panels").GetProperty("console").GetProperty("w").GetInt32());
    }

    [Fact]
    public async Task Reset_RestoresTheDefaultsAndRestartsTheGrid()
    {
        var layout = await CreateInitializedAsync();
        await layout.SetLockedAsync(true);
        await layout.HideAsync("console");
        _calls.Clear();

        await layout.ResetAsync();

        Assert.False(layout.Locked);
        Assert.True(layout["console"].Visible);
        Assert.Equal(["destroy", "saveLayout"], Called);
        await layout.AfterRenderAsync();
        Assert.Equal("init", Called.Last());
    }

    [Fact]
    public async Task SavedLayout_SurvivesAReload()
    {
        var first = await CreateInitializedAsync();
        await first.HideAsync("stats");
        await first.OnLayoutChanged([new PanelPosition("console", 2, 2, 6, 6)]);

        var second = await CreateInitializedAsync();

        Assert.False(second["stats"].Visible);
        Assert.Equal((2, 2), (second["console"].X, second["console"].Y));
    }

    [Fact]
    public async Task ADisconnectedBrowser_DoesNotBreakSaving()
    {
        var layout = await CreateInitializedAsync();
        _module.Setup(m => m.InvokeAsync<IJSVoidResult>("saveLayout", It.IsAny<object?[]?>()))
               .Throws(new JSDisconnectedException("gone"));

        await layout.OnLayoutChanged([new PanelPosition("console", 0, 0, 6, 6)]);
        await layout.DisposeAsync();
    }

    [Fact]
    public async Task WithoutTheModule_ChangesStayInMemory()
    {
        var layout = new DashboardLayoutService(_js.Object);

        await layout.HideAsync("console");
        await layout.SetLockedAsync(true);
        await layout.AfterRenderAsync();
        await layout.DisposeAsync();

        Assert.False(layout["console"].Visible);
        Assert.Empty(_calls);
    }
}
