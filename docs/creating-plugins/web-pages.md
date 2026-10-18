# Plugin web pages

A plugin can add its own pages to the WebUI: a settings screen, a leaderboard, a status page… They are ordinary Razor components inside the plugin DLL. The WebUI mounts them while plugins are loaded and removes them when plugins unload.

- Pages are listed in the header's **Pages** menu and in the **Plugin pages** panel.
- They render inside the dashboard's layout, with a breadcrumb back to the dashboard, in interactive server mode (event handlers, `@bind`, … work).
- They can inject the host's services, including the bot's configuration and logger.

`Plugins/WebPagesDemo` is the reference implementation.

## Project file

Pages need the Razor SDK and a reference to `PluginPages.Abstractions`:

```xml
<Project Sdk="Microsoft.NET.Sdk.Razor">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
        <!-- The host serves only what is inside the DLL: no scoped CSS bundle, no static web assets. -->
        <ScopedCssEnabled>false</ScopedCssEnabled>
    </PropertyGroup>

    <ItemGroup>
        <FrameworkReference Include="Microsoft.AspNetCore.App" />
    </ItemGroup>

    <!-- Provided by the host at runtime: never ship copies. -->
    <ItemGroup>
        <ProjectReference Include="..\..\DiscordBotCore.PluginCore\DiscordBotCore.PluginCore.csproj" Private="false" />
        <ProjectReference Include="..\..\Modules\PluginPages.Abstractions\PluginPages.Abstractions.csproj" Private="false" />
        <!-- Only if the pages inject the bot configuration: -->
        <ProjectReference Include="..\..\DiscordBotCore.Configuration\DiscordBotCore.Configuration.csproj" Private="false" />
    </ItemGroup>

    <!-- Files under wwwroot/ are embedded in the DLL and served at /plugin/<id>/_assets/<path>. -->
    <ItemGroup>
        <Content Remove="wwwroot\**" />
        <EmbeddedResource Include="wwwroot\**" LogicalName="wwwroot/%(RecursiveDir)%(Filename)%(Extension)" />
    </ItemGroup>

</Project>
```

`Private="false"` on `PluginPages.Abstractions` is **required**. If the plugin brings its own copy, the attribute and context types don't match the host's, and every page is rejected with a type identity error.

Add an `_Imports.razor`:

```razor
@using Microsoft.AspNetCore.Components
@using Microsoft.AspNetCore.Components.Web
@using PluginPages
```

## A page

```razor
@attribute [PluginPage("/", Title = "Leaderboard", Icon = "leaderboard", Order = 0)]
@inject DiscordBotCore.Logging.ILogger Logger

<PluginStylesheet Href="site.css"/>

<h1>Leaderboard</h1>
<p>Served at <code>@Context.BasePath</code></p>
<a href="@Context.Href("settings")">Settings</a>

@code {
    [CascadingParameter] public PluginPageContext Context { get; set; } = default!;
}
```

Use `[PluginPage]`, **not `@page`**. The host's router never sees plugin assemblies, so `@page` routes are ignored.

### `[PluginPage]` options

| Property | Meaning |
| --- | --- |
| `route` (constructor) | Route relative to the plugin, starting with `/`. `"/"` is the plugin's index page. |
| `Title` | Name in the menus, the breadcrumb and the browser tab. **Pages without a title aren't listed in menus** (they still work by URL). |
| `Icon` | A [Material Symbols](https://fonts.google.com/icons) name, e.g. `settings`. |
| `Order` | Position among the plugin's pages in menus; lower first. |
| `HideFromMenu` | Keep a titled page out of the menus. |

A component can carry several `[PluginPage]` attributes to answer on several routes.

## URLs

Every page of a plugin lives under `/plugin/<plugin id>/`, where the id is the plugin's GUID in the plugin database. `/settings` in a plugin with id `3f2c…` is served at `/plugin/3f2c…/settings`.

Because the app uses `<base href="/">`, a plain relative link (`href="settings"`) would resolve from the site root. **Build every link with `Context.Href`**:

```razor
<a href="@Context.Href("items/42")">Item 42</a>
<a href="@Context.Href()">Back to the plugin's home</a>
```

and use it with `NavigationManager.NavigateTo(Context.Href("..."))` too.

## Routes and parameters

Routes are ASP.NET Core route templates:

| Route | Matches |
| --- | --- |
| `/` | the plugin's home |
| `/settings` | `settings` |
| `/users/{id:long}` | `users/123`, with `id` = 123 |
| `/search/{term?}` | `search` and `search/abc` |
| `/files/{*path}` | `files/a/b/c.txt`, with `path` = `a/b/c.txt` |

Route values bind to `[Parameter]` properties with the same name (case-insensitive), converted to the property's type. They're also available as strings in `Context.RouteValues`.

```razor
@attribute [PluginPage("/items/{id:int}", Title = "Item")]

<h1>Item @Id</h1>

@code {
    [CascadingParameter] public PluginPageContext Context { get; set; } = default!;
    [Parameter] public int Id { get; set; }
}
```

Pages with route parameters are never listed in menus; link to them from another page.

### Validation

Pages are checked when plugins load. A page is **rejected**, with the reason shown in the **Plugin pages** panel and in the console, when:

- the route is empty, doesn't start with `/`, contains `?`, `#`, `\`, `//`, `.` or `..` segments, or isn't a valid template;
- its first segment is `_assets` (reserved for embedded files);
- an inline constraint is unknown or invalid;
- the type isn't a public, non-abstract, non-generic Razor component with a public parameterless constructor;
- the route **overlaps** another route of the same plugin, i.e. they match the same paths once parameter names are ignored (`/items/{id:int}` and `/items/{number}`). The first type by full name keeps the route.

`Plugins/WebPagesDemo/Pages/ConflictingItem.razor` shows a rejected overlap once you uncomment its attribute.

## `PluginPageContext`

Cascaded to every plugin page:

| Member | Meaning |
| --- | --- |
| `PluginKey` | The plugin id used in the URL. |
| `PluginName` | The plugin's name. |
| `BasePath` | `/plugin/<id>`, without a trailing slash. |
| `AssetsPath` | `/plugin/<id>/_assets`. |
| `RelativePath` | The requested path relative to `BasePath`, e.g. `/items/42`. |
| `Title` | The matched route's `Title`. |
| `RouteValues` | Raw route values by name. |
| `Href(path)` | Absolute URL of another page of the plugin. |
| `AssetUrl(path)` | Versioned URL of an embedded file. |

## Static files

Put CSS, JavaScript, images, … under `wwwroot/` in the plugin project. With the `EmbeddedResource` item above, they're compiled into the DLL and served at `/plugin/<id>/_assets/<path>`.

- **CSS**: `<PluginStylesheet Href="css/site.css"/>` adds a `<link>` to the document head while the page is shown.
- **Images and other files**: `<img src="@Context.AssetUrl("img/logo.png")">`.
- **JavaScript modules**:

  ```csharp
  @inject IJSRuntime JS

  @code {
      protected override async Task OnAfterRenderAsync(bool firstRender)
      {
          if (firstRender)
          {
              var module = await JS.InvokeAsync<IJSObjectReference>("import", Context.AssetUrl("js/chart.js"));
              await module.InvokeVoidAsync("draw");
          }
      }
  }
  ```

`AssetUrl` adds a version query string, so browsers don't keep a stale file after you update the plugin.

**CSS isolation (`.razor.css`) is not supported**; keep `ScopedCssEnabled` off and use a stylesheet. The dashboard uses Radzen's Material theme; to blend in, use its CSS variables (`var(--rz-primary)`, `var(--rz-text-color)`, `var(--rz-base-background-color)`, …).

## Host services

Pages are created by the host's dependency injection, so they can `@inject`:

| Service | Use |
| --- | --- |
| `DiscordBotCore.Logging.ILogger` | Log to the dashboard console. |
| `DiscordBotCore.Configuration.IConfiguration` | Read or change `config.json` (call `SaveToFile()` to persist). |
| `DiscordBotCore.PluginManagement.IPluginManager` | The installed-plugin list. |
| `DiscordBotCore.PluginManagement.Loading.IPluginLoader` | Loaded commands, events, slash commands. |
| `DiscordBotCore.Bot.IDiscordBotApplication` | The bot; `Client` is the `DiscordSocketClient` while it runs. |
| `NavigationManager`, `IJSRuntime`, Radzen services, … | As in any Blazor app. |

Reference the matching projects with `Private="false"`.

## Lifecycle

- Pages are registered after **Load plugins** and removed before unload. An open plugin page then shows *This plugin isn't loaded* until plugins are loaded again.
- The plugin id (and so the URL) stays the same across reloads, as long as the plugin isn't deleted and added again.
- To let the plugin's assembly be unloaded, the host clears Blazor's internal caches for plugin components. Avoid keeping references to plugin types in static host-side places, and dispose timers and event subscriptions in `Dispose`/`DisposeAsync` (implement `IDisposable` with `@implements IDisposable`).
