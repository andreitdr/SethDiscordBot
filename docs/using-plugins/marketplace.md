# Marketplace and plugin repository

The **Marketplace** panel lists plugins published on a **plugin repository**: a separate HTTP server that serves plugin metadata and download links. This repository doesn't include that server; you point the bot at one you run or trust.

## Configure the repository

No repository is configured by default. The Marketplace panel then shows **No plugin repository configured** and makes no requests; plugins added with **Add local** still work. To use a server, add this to [`config.json`](../configuration/bot-configuration.md#marketplace-repository) and restart the WebUI:

```json
"RemotePluginConnectionConfigurationDetails": {
  "BaseUrl": "https://plugins.example.com/api/v1/",
  "PluginsEndpoint": "plugin/",
  "DependenciesEndpoint": "dependency/"
}
```

`BaseUrl` must be an absolute `http://` or `https://` URL; anything else counts as not configured. `PluginsEndpoint` and `DependenciesEndpoint` default to `plugin/` and `dependency/` when left out.

> Installing a plugin downloads a DLL and runs it inside the bot. Only use a repository you trust.

## What the Marketplace shows

Only **approved** plugins built for the **current operating system** (Windows, Linux or macOS) are listed. A plugin whose name is already installed shows **Installed**.

## Installing

**Install**:

1. fetches the plugin's details by id,
2. downloads the DLL to `<PluginFolder>/<name>.dll`,
3. downloads each dependency to `./Libraries/<name>/<dependency location>`, in parallel,
4. adds the plugin to the database, enabled, and records its *executable* dependencies.

If a plugin with the same name is already installed, Install only proceeds when the marketplace version is newer (versions are numeric `major.minor.patch`); it then replaces the old database entry and keeps its enabled state. Installing the same or an older version fails with *Versions are the same* or *Current version is newer*, and a version that isn't numeric `major.minor.patch` (such as `1.0.0-beta`) fails with *Invalid version*.

Then load or reload plugins. The ⓘ button in **Installed plugins** shows the repository's details of a marketplace plugin.

## Repository API

A plugin repository must answer these `GET` requests. `{BaseUrl}`, `{PluginsEndpoint}` and `{DependenciesEndpoint}` come from the configuration; the paths are concatenated as is.

| Request | Returns |
| --- | --- |
| `{BaseUrl}{PluginsEndpoint}get-all-plugins?operatingSystem={os}&includeNotApproved=False` | array of plugins |
| `{BaseUrl}{PluginsEndpoint}get-by-id?pluginId={id}&includeNotApproved=false` | one plugin |
| `{BaseUrl}{PluginsEndpoint}get-by-name?pluginName={name}&operatingSystem={os}&includeNotApproved=False` | one plugin |
| `{BaseUrl}{DependenciesEndpoint}get-by-plugin-id?pluginId={id}` | array of dependencies |

Query values are URL-encoded (a plugin name with a space is sent as `My%20Plugin`).

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
