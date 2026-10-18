# The `Data` folder

Everything the bot stores lives under `./Data`, **relative to the working directory of the process**:

| How you run it | Working directory | Data folder |
| --- | --- | --- |
| `dotnet run --project WebUI` | `WebUI/` | `WebUI/Data/` |
| `dotnet WebUI.dll` from `publish/` | `publish/` | `publish/Data/` |
| Docker | `/app` | `/app/Data` (mounted from `./Data`) |

## Layout

```text
Data/
├── Logs/
│   └── 2026-10-18.log          one file per day
├── Plugins/
│   ├── LevelingSystem.dll      plugin DLLs (uploaded or downloaded)
│   └── PollMaker.dll
└── Resources/
    ├── config.json             bot configuration
    ├── plugins.db              installed-plugin database (SQLite)
    ├── LevelingSystem/         data folder of an event plugin (named after its assembly)
    └── level/                  data folder of a command (named after IDbCommand.Command)
```

Next to the application binaries there can also be a `Libraries/` folder with the extra assemblies of plugins: `Libraries/<plugin assembly name>/*.dll`. See [Dependencies](../creating-plugins/dependencies.md).

All of these paths can be changed: `PluginFolder`, `ResourcesFolder` and `PluginDatabase` in [`config.json`](bot-configuration.md), and `ConfigFile` and `Logger:LogFolder` in the [host settings](host-settings.md).

## The plugin database

`plugins.db` is the list of installed plugins. **The bot only loads plugins that have a row here and are enabled.** Dropping a DLL into `Data/Plugins` is not enough: add it through the dashboard ([Add local](../using-plugins/managing-plugins.md#add-a-plugin-you-built)) so it gets a row.

It has two tables:

```sql
CREATE TABLE Plugins (
    id TEXT PRIMARY KEY,                       -- GUID; also the plugin's web page id
    name TEXT UNIQUE NOT NULL,
    version TEXT NOT NULL,                     -- major.minor.patch
    file_path TEXT NOT NULL,                   -- path of the DLL
    is_offline_added INTEGER NOT NULL DEFAULT 0, -- 1 = added from a local file
    is_enabled INTEGER NOT NULL DEFAULT 1
);

CREATE TABLE Dependencies (                    -- executable dependencies of marketplace plugins
    id TEXT PRIMARY KEY,
    name TEXT UNIQUE NOT NULL,
    file_path TEXT NOT NULL,
    plugin_id TEXT NOT NULL REFERENCES Plugins (id) ON DELETE CASCADE
);
```

## Backups

To back up the bot, stop the WebUI and copy the whole `Data` folder (and `Libraries`, if you use it). `config.json` contains the bot token, so store backups privately.
