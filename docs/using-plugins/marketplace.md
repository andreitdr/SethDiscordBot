# Marketplace and plugin repository

The **Marketplace** panel lists plugins published on a **plugin repository**: a separate HTTP server that serves plugin metadata and download links. This repository doesn't include that server; you point the bot at one you run or trust.

## Configure the repository

By default the bot uses `http://localhost:8080/api/v1/`, with the endpoints `plugin/` and `dependency/`. To use another server, add this to [`config.json`](../configuration/bot-configuration.md#marketplace-repository) and restart the WebUI:

```json
"RemotePluginConnectionConfigurationDetails": {
  "BaseUrl": "https://plugins.example.com/api/v1/",
  "PluginsEndpoint": "plugin/",
  "DependenciesEndpoint": "dependency/"
}
```

> Installing a plugin downloads a DLL and runs it inside the bot. Only use a repository you trust.

## What the Marketplace shows

Only **approved** plugins built for the **current operating system** (Windows, Linux or macOS) are listed. A plugin whose name is already installed shows **Installed**.

## Installing

**Install**:

1. fetches the plugin's details by id,
2. downloads the DLL to `<PluginFolder>/<name>.dll`,
3. downloads each dependency to `./Libraries/<name>/<dependency location>`, in parallel,
4. adds the plugin to the database, enabled, and records its *executable* dependencies.

If a plugin with the same name is already installed, Install only proceeds when the marketplace version is newer (versions are `major.minor.patch`); it then replaces the old database entry and keeps its enabled state. Installing the same or an older version fails with *Versions are the same* or *Current version is newer*.

Then load or reload plugins. The ⓘ button in **Installed plugins** shows the repository's details of a marketplace plugin.

## Repository API

A plugin repository must answer these `GET` requests. `{BaseUrl}`, `{PluginsEndpoint}` and `{DependenciesEndpoint}` come from the configuration; the paths are concatenated as is.

| Request | Returns |
| --- | --- |
| `{BaseUrl}{PluginsEndpoint}get-all-plugins?operatingSystem={os}&includeNotApproved=False` | array of plugins |
| `{BaseUrl}{PluginsEndpoint}get-by-id?pluginId={id}&includeNotApproved=false` | one plugin |
| `{BaseUrl}{PluginsEndpoint}get-by-name?pluginName={name}&operatingSystem={os}&includeNotApproved=False` | one plugin |
| `{BaseUrl}{DependenciesEndpoint}get-by-plugin-id?pluginId={id}` | array of dependencies |

`operatingSystem` is `0` for Windows, `1` for Linux and `2` for macOS. A non-success status code means "none" (an empty list, or no plugin).

### Plugin object

```json
{
  "id": 7,
  "name": "LevelingSystem",
  "description": "Gives users experience for chatting",
  "version": "1.0.0",
  "author": "andreitdr",
  "downloadLink": "https://plugins.example.com/files/LevelingSystem.dll",
  "operatingSystem": 1,
  "isApproved": true
}
```

`version` must be `major.minor.patch`. Property names are matched case-insensitively.

### Dependency object

```json
{
  "dependencyName": "SomeLibrary",
  "dependencyLink": "https://plugins.example.com/files/SomeLibrary.dll",
  "dependencyLocation": "SomeLibrary.dll",
  "isExecutable": false
}
```

- `dependencyLocation` is the path relative to `Libraries/<plugin name>/`, and may contain sub-folders.
- `isExecutable: true` records the dependency in the database, so the plugin can find its path later with `IPluginManager.GetDependencyLocation(dependencyName, pluginName)`. Use it for programs or native libraries the plugin starts or loads itself.
