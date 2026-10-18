# Developing the bot itself

For changes to the bot, the dashboard or the modules (as opposed to writing a plugin).

## Build and test

```bash
dotnet build
```

```bash
dotnet test
```

Run one test:

```bash
dotnet test Tests/Tests.DiscordBotCore.Configuration --filter "FullyQualifiedName~ConfigurationTests.Add_ShouldAddKeyValuePair"
```

| Test project | Covers |
| --- | --- |
| `Tests.DiscordBotCore.Configuration` | `ConfigurationBase` (through a `TestableConfigurationBase` subclass). |
| `Tests.DiscordBotCore.Logging` | The logger. |
| `Tests.PluginPages` | Route parsing and matching, registry validation, the WebPagesDemo pages, and that unregistering pages releases the plugin's load context. |

Tests use xUnit and Moq.

## Run the dashboard

```bash
dotnet run --project WebUI
```

<http://localhost:5028>, with its data in `WebUI/Data/`. To try changes without touching that data, point the host to another config file, whose `PluginFolder`, `ResourcesFolder` and `PluginDatabase` point elsewhere:

```bash
dotnet run --project WebUI -- --ConfigFile=/tmp/seth-test/config.json
```

## Adding a dashboard panel

1. Create a Radzen component in `WebUI/Components/Panels/` that inherits `PanelBase`.
2. Add one line to `PanelRegistry.All` in `WebUI/Components/Dashboard/PanelRegistry.cs`: id, title, Material icon, component type, default grid position and size (12-column grid), minimum size.

Rules that keep the dashboard consistent:

- Panels don't call core services for actions. Use (or extend) `BotStateService`, `PluginCatalogService` or `LogStreamService`; they're shared by every tab, raise `Changed`, and return `IResponse<T>` instead of throwing. In the panel, call `Observe(service)` to re-render on changes, and `Report(...)` to show the result.
- Don't touch the grid's DOM. Showing, hiding and moving panels go through `DashboardLayoutService`, which keeps Gridstack.js and Blazor in step.
- Style with Radzen's `--rz-*` CSS variables in `wwwroot/app.css`, so both themes work.

The layout saved in browsers is keyed `seth.dashboard.layout.v1`; a new panel appears at its default position for existing layouts.

## Plugin pages host

The host side is reusable and has no Discord dependency (`Modules/PluginPages.Hosting`):

- `services.AddPluginPages()` registers the registry.
- `app.MapPluginPageAssets()` serves embedded files at `/plugin/{key}/_assets/{**path}`.
- One routable page renders `PluginPageOutlet` at `/plugin/{PluginKey}/{*PluginPath}` (`WebUI/Components/Pages/PluginPage.razor`). Don't add other WebUI pages under `/plugin`.
- Something calls `Register(pluginKey, pluginName, assembly)` after loading and `Clear()` before unloading (`WebUI/Services/PluginPagesBridge.cs`).
