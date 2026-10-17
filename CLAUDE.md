# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

```bash
dotnet build                      # build the whole solution (SethDiscordBot.sln)
dotnet test                       # run all xUnit test projects under Tests/
dotnet test Tests/Tests.DiscordBotCore.Configuration --filter "FullyQualifiedName~ConfigurationTests.Add_ShouldAddKeyValuePair"
dotnet run --project WebUI        # host app; http://localhost:5028 (launchSettings "http" profile)
docker compose up --build         # builds WebUI/Dockerfile, mounts ./Data into /app/Data, port 8080
```

Native part of the C++ demo plugin (CMake, C++17, produces a shared library):

```bash
cmake -S Plugins/CppModuleDemo/Native -B Plugins/CppModuleDemo/Native/build && cmake --build Plugins/CppModuleDemo/Native/build
```

All projects target **net10.0** (`global.json` pins SDK 10.0.0 with `rollForward: latestMajor`). The README, `.github/workflows/dotnet.yml` and `WebUI/Dockerfile` still reference .NET 8 and are out of date; the README's plugin examples also use the old `PluginManager`/`DBCommand` API — the current API is described below.

## Architecture

The bot is a library (`DiscordBotCore`) hosted by a Blazor Server app (`WebUI`). There is no console entry point: `WebUI/Initializer.cs` (`AddDiscordBotComponents`) wires every core service as a DI singleton, and the bot is started/stopped and plugins are loaded from the single-page WebUI dashboard.

### WebUI dashboard

- One page (`Components/Pages/Dashboard.razor`) of draggable/resizable panels. Panels are listed in `Components/Dashboard/PanelRegistry.cs` (id, title, icon, component, default grid position); each panel is a Radzen component in `Components/Panels/` deriving from `PanelBase`.
- Gridstack.js (vendored in `wwwroot/lib/gridstack`, glue in `wwwroot/js/dashboard.js`) only moves and resizes panels; Blazor renders them. `DashboardLayoutService` (scoped, per browser) owns positions, visibility, collapse and lock state and saves them in localStorage (`seth.dashboard.layout.v1`). Hiding a panel must release it from gridstack before Blazor removes the element, and showing one adopts it after render; go through the service rather than touching the DOM.
- Components never call core services directly for actions: `BotStateService` (start/stop with a 30s watchdog, plugin load/reload, gateway stats), `PluginCatalogService` (installed/online lists, install/uninstall/enable/add-local) and `LogStreamService` (throttled log updates) are singletons shared by every tab, raise `Changed`, and return `IResponse<T>` instead of throwing.
- Theme: Radzen `material` / `material-dark`, remembered with Radzen's cookie theme service (`SethDiscordBot.Theme`). Custom CSS in `wwwroot/app.css` uses only `--rz-*` tokens. Static assets are fingerprinted (`MapStaticAssets` + `@Assets[...]`), and the app renders interactive server without prerendering.

### Project layering

- `DiscordBotCore.Logging` / `DiscordBotCore.Configuration` / `DiscordBotCore.Utilities` / `DiscordBotCore.Networking` / `DiscordBotCore.Database.Sqlite` / `DiscordBotCore.Resources` — foundation libraries. `Resources` holds the default config keys and default paths (everything lives under `./Data/...`, relative to the process working directory).
- `DiscordBotCore.PluginCore` — the **plugin contract** (`IDbCommand`, `IDbEvent`, `IDbSlashCommand` plus their execution-argument types). Plugins should reference this, not the bot host.
- `DiscordBotCore.PluginManagement` — `PluginManager`: tracks installed plugins in a SQLite DB (`Data/Resources/plugins.db`, tables `Plugins` and `Dependencies`) and downloads plugins/dependencies from a remote repository (`PluginRepository`, configured via `RemotePluginConnectionConfigurationDetails`).
- `DiscordBotCore.PluginManagement.Loading` — `PluginLoader`: loads enabled plugin DLLs into a collectible `AssemblyLoadContext` and instantiates types by reflection.
- `DiscordBotCore` — `DiscordBotApplication` (Discord.Net socket client lifecycle), `CommandHandler` (prefix/mention parsing and dispatch), built-in `HelpCommand`.
- `Modules/CppCompatibilityModule` — helper library for plugins that call into native shared libraries via function pointers/delegates.
- `Modules/PluginPages.Abstractions` / `Modules/PluginPages.Hosting` — reusable runtime plugin web pages (see "Plugin web pages").
- `Plugins/*` — sample plugins (LevelingSystem, PollMaker, CppModuleDemo, WebPagesDemo).

### Plugin lifecycle (spans several projects)

1. A plugin is a class library with any number of public types implementing `IDbCommand`, `IDbEvent` or `IDbSlashCommand` (parameterless constructor; created with `Activator.CreateInstance`).
2. **Plugins are discovered from the SQLite DB, not by scanning `Data/Plugins`.** A DLL only loads if it has a row in `Plugins` with `is_enabled = 1`. Rows are added by `PluginManager.InstallPlugin` (remote) or the WebUI "Add Local" page (`CreateOfflineLocalPlugin` + `AppendPluginToDatabase`).
3. Load order matters: `DiscordBotApplication.StartAsync` logs in, and the `LoggedIn` handler calls `PluginLoader.SetDiscordClient`. `PluginLoader.LoadPlugins` must run **after** that — events and slash commands bail out when no client is set, and slash commands also need at least one guild.
4. `LoadPlugins` starts events first (`IDbEvent.Start` gets the client, prefix and a per-plugin directory), then registers `HelpCommand` from the host assembly, then commands, then slash commands.
5. Slash commands are registered per guild for each ID in the `ServerIds` config list, or globally if that list is empty. `UnloadAllPlugins` deletes those application commands and unloads the load context.

Per-plugin data directories: events get `<ResourcesFolder>/<plugin assembly name>`, commands get `<ResourcesFolder>/<IDbCommand.Command>`.

### Plugin web pages

A plugin's main DLL can also contain Razor components that the WebUI mounts while plugins are loaded. The mechanism lives in two reusable modules with no Discord dependency: `Modules/PluginPages.Abstractions` (what plugins reference) and `Modules/PluginPages.Hosting` (host side).

- A page is a public component with `[PluginPage("/route", Title = …, Icon = …)]` (or `@attribute [PluginPage(...)]` in a `.razor` file). Don't use `@page` in plugins; the host router never sees plugin assemblies. Routes are ASP.NET Core templates relative to the plugin: `/settings` is served at `/plugin/<LocalPlugin.Id>/settings`, and `/` is the plugin's index page.
- The WebUI has one catch-all page, `Components/Pages/PluginPage.razor` (`/plugin/{PluginKey}/{*PluginPath}`), which renders `PluginPageOutlet`. The outlet resolves the component through `IPluginPageRegistry` and renders it with `DynamicComponent`. Don't add other WebUI pages under `/plugin`.
- `PluginLoader` raises `PluginsLoaded` (after load, with `LoadedPlugins` = each plugin's `LocalPlugin` + main `Assembly`) and `PluginsUnloading` (before the load context unloads). `WebUI/Services/PluginPagesBridge.cs` registers and clears the pages on those events. On clear, the registry purges Blazor's static per-type caches (`PluginComponentCaches`) so the collectible context can be collected; `Tests.PluginPages` has a test that checks this.
- Validation happens per plugin at registration: the route syntax, the reserved first segment `_assets`, that inline constraints resolve, the component type itself, and overlaps (routes with the same shape after normalizing parameters). The first type by full name wins an overlap. Rejections are logged and listed in the Plugin pages panel.
- Pages get a cascading `PluginPageContext` (`Href(...)` for links, since the app uses `<base href="/">`, and `AssetUrl(...)`). Host services can be injected. Route values bind to `[Parameter]` properties by name.
- Static files are embedded resources with logical names `wwwroot/...`, served at `/plugin/<id>/_assets/<path>`. Use `<PluginStylesheet Href="x.css"/>` for CSS and `AssetUrl` for JS module imports. CSS isolation (`.razor.css`) is not supported.
- Reference the contract assemblies with `Private="false"`. A plugin that ships its own copy of `PluginPages.Abstractions` gets its pages rejected with a type identity error. `Plugins/WebPagesDemo` is the reference csproj and the reference set of pages.

### Native dependencies

`WebUI/Program.cs` hooks `AppDomain.AssemblyResolve` to look in `<app dir>/Libraries/<requesting assembly name>/` for `.dll`/`.so`/`.dylib`. `PluginManager` installs plugin dependencies to the matching `./Libraries/<PluginName>/...` path. The CppModuleDemo plugin instead loads its native library from its event resources directory (`libCppModuleDemo.dylib`/`.so` or `CppModuleDemo.dll`).

### Configuration

`DiscordBotCore.Configuration.Configuration` is a JSON-backed dictionary (`Data/Resources/config.json`, Newtonsoft.Json). It is created with auto-add enabled, so calling `Get<T>(key, default)` writes the default into the in-memory config (persisted on `SaveToFile`). Keys read at runtime include `token`, `prefix` (default `;`), `ServerIds`, `PluginFolder`, `ResourcesFolder`, `PluginDatabase`. A 401 on disconnect clears `token` and saves the file.

### Conventions

- Fallible operations return `IResponse<T>` / `Response<T>.Success(...)` / `Response.Failure(...)` from `DiscordBotCore.Utilities.Responses` rather than throwing.
- Log through the project's own `DiscordBotCore.Logging.ILogger` (`Log(message, sender, LogType)`, `LogException`). In WebUI it clashes with `Microsoft.Extensions.Logging.ILogger`, hence the `using ILogger = DiscordBotCore.Logging.ILogger;` aliases.
- Private fields use an `_PascalCase` prefix (`_Logger`, `_PluginManager`).
- Tests use xUnit + Moq; `ConfigurationTests` tests `ConfigurationBase` through a `TestableConfigurationBase` subclass.
