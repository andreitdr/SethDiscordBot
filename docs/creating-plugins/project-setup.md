# Project setup

## Where to put the project

The bot's libraries aren't published as NuGet packages, so a plugin references them as projects (or as built DLLs).

**Inside the repository (recommended).** Put the plugin under `Plugins/` like the samples. Relative project references stay short and `dotnet build` at the root builds it with everything else. Optionally add it to the solution:

```bash
dotnet sln add Plugins/MyPlugin/MyPlugin.csproj --solution-folder Plugins
```

**In your own repository.** Clone SethDiscordBot next to it and use relative paths:

```xml
<ProjectReference Include="..\SethDiscordBot\DiscordBotCore.PluginCore\DiscordBotCore.PluginCore.csproj" Private="false" />
```

or reference built DLLs from a WebUI build:

```xml
<Reference Include="DiscordBotCore.PluginCore">
  <HintPath>..\SethDiscordBot\WebUI\bin\Debug\net10.0\DiscordBotCore.PluginCore.dll</HintPath>
  <Private>false</Private>
</Reference>
<PackageReference Include="Discord.Net" Version="3.18.0" />
```

With DLL references, add the Discord.Net package yourself, at the **same version** the bot uses (see `DiscordBotCore.PluginCore.csproj`).

## The project file

A plugin with commands, events and slash commands:

```xml
<Project Sdk="Microsoft.NET.Sdk">

    <PropertyGroup>
        <TargetFramework>net10.0</TargetFramework>
        <ImplicitUsings>enable</ImplicitUsings>
        <Nullable>enable</Nullable>
    </PropertyGroup>

    <ItemGroup>
        <!-- The plugin contract: IDbCommand, IDbEvent, IDbSlashCommand, Discord.Net and ILogger. -->
        <ProjectReference Include="..\..\DiscordBotCore.PluginCore\DiscordBotCore.PluginCore.csproj" Private="false" />
    </ItemGroup>

</Project>
```

- **`net10.0`**: the plugin runs inside the bot's .NET 10 process.
- **`Private="false"`**: the bot already provides these assemblies at runtime. This keeps them out of your build output so you never ship a second copy, which would have a different type identity. It's required for [web pages](web-pages.md), and good practice everywhere.

A plugin with web pages uses the Razor SDK instead; see [Web pages](web-pages.md#project-file).

## What you can reference

The bot process already contains these assemblies, so a plugin can reference them (with `Private="false"`) and use them without shipping anything:

| Assembly | Gives you |
| --- | --- |
| `DiscordBotCore.PluginCore` | The plugin interfaces and execution arguments. |
| `Discord.Net` (3.18) | The Discord API: `DiscordSocketClient`, embeds, components, … |
| `DiscordBotCore.Logging` | `ILogger`, `LogType`. Comes with PluginCore. |
| `DiscordBotCore.Database.Sqlite` | `SqlDatabase`, a small SQLite helper ([Storage](storage-and-logging.md#a-sqlite-database)). |
| `DiscordBotCore.Utilities` | `JsonManager`, `Response`/`IResponse`, `Result`, `OperatingSystem`. |
| `DiscordBotCore.Configuration` | `IConfiguration` (the bot's `config.json`). |
| `DiscordBotCore.PluginManagement` | `IPluginManager`, `LocalPlugin`, … |
| `DiscordBotCore` | `DiscordBotApplication`, `DiscordPermissions` extensions (`IsAdmin()`). |
| `PluginPages.Abstractions` | `[PluginPage]`, `PluginPageContext`, `PluginStylesheet`. |
| `Newtonsoft.Json`, `Microsoft.Data.Sqlite`, the ASP.NET Core shared framework, `Radzen.Blazor` | As the bot uses them. |

Anything else (another NuGet package, `Modules/CppCompatibilityModule`, a library of your own) is **not** in the bot. Only your main DLL is uploaded, so those assemblies must be placed in the `Libraries` folder. See [Dependencies](dependencies.md).

## Naming

- Use the **assembly name as the plugin name** when you add the plugin. Dependency folders (`Libraries/<name>`) and event data folders (`Data/Resources/<assembly name>`) are keyed by it.
- Give each plugin its own root namespace. Several plugins are loaded into the same load context, so two plugins with the same assembly name would clash.

## Recommended layout

```text
Plugins/MyPlugin/
├── MyPlugin.csproj
├── Commands/
│   └── StatsCommand.cs
├── Events/
│   └── WelcomeEvent.cs
├── SlashCommands/
│   └── RemindSlashCommand.cs
└── Internal/
    └── State.cs          shared state and helpers
```

---

Next: [Commands](commands.md)
