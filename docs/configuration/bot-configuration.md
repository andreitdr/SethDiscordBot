# Bot configuration (`config.json`)

The bot's own settings live in a JSON file, by default `Data/Resources/config.json`. The dashboard's **Settings** panel edits the most common keys; you can also edit the file by hand while the WebUI is stopped.

## Example

```json
{
  "token": "<your bot token>",
  "prefix": ";",
  "ServerIds": [
    123456789012345678
  ],
  "PluginFolder": "./Data/Plugins",
  "ResourcesFolder": "./Data/Resources",
  "PluginDatabase": "./Data/Resources/plugins.db"
}
```

You don't need to create this file. Missing keys are added with their default values the first time they are read, and written to disk on the next save.

## Keys

| Key | Default | Used for | When it is read |
| --- | --- | --- | --- |
| `token` | *(none)* | The bot token. Set it in **Settings**. | When the bot starts. If Discord rejects it (HTTP 401) on disconnect, the bot clears it and saves the file. |
| `prefix` | `;` | The text that starts a prefix command. Up to 5 characters in the dashboard. | On every message, and when events start (they receive it in `BotPrefix`). |
| `ServerIds` | `[]` | IDs of the servers to register slash commands on. Empty means global registration. | When plugins load. |
| `PluginFolder` | `./Data/Plugins` | Where plugin DLLs are stored (uploads and marketplace downloads). | When a plugin is added or installed. |
| `ResourcesFolder` | `./Data/Resources` | The parent of each plugin's private data folder. | When plugins load and commands run. |
| `PluginDatabase` | `./Data/Resources/plugins.db` | The SQLite database that lists installed plugins. | At startup. |
| `RemotePluginConnectionConfigurationDetails` | see below | Where the marketplace downloads plugins from. | At startup. |

Relative paths are resolved from the process working directory (see [The Data folder](data-folder.md)).

## Marketplace repository

The marketplace talks to a plugin repository server. Without configuration it uses `http://localhost:8080/api/v1/`. To point it elsewhere, add:

```json
"RemotePluginConnectionConfigurationDetails": {
  "BaseUrl": "https://plugins.example.com/api/v1/",
  "PluginsEndpoint": "plugin/",
  "DependenciesEndpoint": "dependency/"
}
```

All three keys are required when the object is present. Keep the trailing slashes: endpoint URLs are built by concatenation, for example `BaseUrl` + `plugin/` + `get-all-plugins`. The repository's API is described in [Marketplace and plugin repository](../using-plugins/marketplace.md#repository-api).

## Changing the config file location

The location of `config.json` itself is a host setting, `ConfigFile`. See [Host settings](host-settings.md).

## Reading the configuration from a plugin

Plugins can read (and write) the same configuration through `DiscordBotCore.Configuration.IConfiguration`, which is available to plugin web pages by dependency injection. See [Storage, configuration and logging](../creating-plugins/storage-and-logging.md#reading-the-bot-configuration).
