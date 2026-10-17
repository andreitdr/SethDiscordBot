using WebUI.Components.Panels;

namespace WebUI.Components.Dashboard;

/// <summary>
/// Describes a panel that can be placed on the dashboard. Positions are in grid cells (12 columns).
/// </summary>
public sealed record PanelDefinition(
    string Id,
    string Title,
    string Icon,
    Type ComponentType,
    int X, int Y, int W, int H,
    int MinW = 2, int MinH = 2);

public static class PanelRegistry
{
    /// <summary>Every panel, in default layout order. Adding a panel is one line here plus its component.</summary>
    public static readonly IReadOnlyList<PanelDefinition> All =
    [
        new("status", "Bot", "smart_toy", typeof(StatusPanel), X: 0, Y: 0, W: 3, H: 4, MinW: 3, MinH: 3),
        new("stats", "Gateway & servers", "monitoring", typeof(StatsPanel), X: 0, Y: 4, W: 3, H: 5, MinW: 3, MinH: 3),
        new("console", "Console", "terminal", typeof(ConsolePanel), X: 3, Y: 0, W: 6, H: 9, MinW: 4, MinH: 3),
        new("settings", "Settings", "settings", typeof(SettingsPanel), X: 9, Y: 0, W: 3, H: 9, MinW: 3, MinH: 4),
        new("installed", "Installed plugins", "extension", typeof(InstalledPluginsPanel), X: 0, Y: 9, W: 7, H: 6, MinW: 4, MinH: 3),
        new("loaded", "Loaded components", "deployed_code", typeof(LoadedComponentsPanel), X: 7, Y: 9, W: 5, H: 6, MinW: 3, MinH: 3),
        new("marketplace", "Marketplace", "storefront", typeof(MarketplacePanel), X: 0, Y: 15, W: 12, H: 6, MinW: 4, MinH: 3),
    ];

    public static PanelDefinition? Find(string id) => All.FirstOrDefault(p => p.Id == id);
}
