# Storage, configuration and logging

## Logging

Every execution argument carries an `ILogger` (`DiscordBotCore.Logging`). Messages go to the dashboard **Console**, the process console and the daily log file in `Data/Logs`.

```csharp
args.Logger.Log("Something happened", this);                     // Info, sender = this component
args.Logger.Log("Quota almost used", this, LogType.Warning);
args.Logger.Log("Could not reach the API", this, LogType.Error);
args.Logger.LogException(ex, this);                              // message of the exception
args.Logger.LogException(ex, this, logFullStack: true);          // with the stack trace
```

`LogType` is `Info`, `Warning`, `Error` or `Critical`. The `sender` (any object, or a `Type` from static code) is shown next to the message in the console, so pass `this` to make your plugin's lines easy to filter.

For slash commands, the logger is the first parameter of each `Execute…` method.

## Your plugin's folder

| Component | `PluginBaseDirectory` | Created for you |
| --- | --- | --- |
| Event | `Data/Resources/<plugin assembly name>` | Yes, before `Start` |
| Command | `Data/Resources/<IDbCommand.Command>` | No, call `args.PluginBaseDirectory.Create()` |
| Slash command | none: use the folder of an event in the same plugin, or a path you choose under `Data/Resources` | — |

(`Data/Resources` is the `ResourcesFolder` setting.) Files there survive reloads, restarts and even deleting the plugin, so they're the place for settings and data.

```csharp
string file = Path.Combine(args.PluginBaseDirectory.FullName, "settings.json");
```

## JSON settings

`DiscordBotCore.Utilities.JsonManager` (System.Text.Json) reads and writes JSON files:

```csharp
using DiscordBotCore.Utilities;

public class MySettings
{
    public int CooldownSeconds { get; set; } = 5;
    public List<ulong> IgnoredChannels { get; set; } = [];
}

string path = Path.Combine(args.PluginBaseDirectory.FullName, "settings.json");

MySettings settings = File.Exists(path)
    ? await JsonManager.ConvertFromJson<MySettings>(path)   // accepts a file path or a JSON string
    : new MySettings();

await JsonManager.SaveToJsonFile(path, settings);
```

Reference `DiscordBotCore.Utilities` with `Private="false"`; the bot already ships it.

## A SQLite database

`DiscordBotCore.Database.Sqlite.SqlDatabase` wraps one SQLite file:

```csharp
using DiscordBotCore.Database.Sqlite;

var db = new SqlDatabase(Path.Combine(args.PluginBaseDirectory.FullName, "data.db"));
await db.Open();

if (!await db.TableExistsAsync("Points"))
    await db.CreateTableAsync("Points", "UserId TEXT", "Points INT");

// Parameterized queries: never concatenate user input into SQL.
await db.ExecuteNonQueryAsync("INSERT INTO Points (UserId, Points) VALUES (@id, @points)",
    new KeyValuePair<string, object>("id", userId.ToString()),
    new KeyValuePair<string, object>("points", 10));

object[]? row = await db.ReadDataArrayAsync("SELECT * FROM Points WHERE UserId = @id",
    new KeyValuePair<string, object>("id", userId.ToString()));
```

Other helpers: `ReadDataAsync` (the first row, its values joined by spaces), `ReadAllRowsAsync` (every row, as text), `ReadObjectOfTypeAsync<T>` / `ReadListOfTypeAsync<T>` (map rows with a converter), `InsertAsync`, `KeyExistsAsync`, `SetValueAsync`, `GetValueAsync`, `RemoveKeyAsync`, `AddColumnsToTableAsync`. Integer columns come back as `long`, and a `null` parameter is stored as `NULL`.

The helpers send values as parameters, so quotes in user input are safe. Table and column names are written into the SQL as given: never take them from user input.

Open the database once (in an event's `Start`) and share it through a static field, as `Plugins/LevelingSystem` does. Close it in the event's `Stop` with `await db.CloseAsync()` (or `DisposeAsync`), so a reload doesn't leave the file open.

## Reading the bot configuration

Commands, events and slash commands don't receive the bot configuration. What they usually need is in their arguments already (`BotPrefix` for events, the client for everything else).

[Plugin web pages](web-pages.md) can inject it, since they are created by the host's dependency injection:

```razor
@inject DiscordBotCore.Configuration.IConfiguration Configuration

<p>Prefix: @Configuration.Get("prefix", ";")</p>
```

Note that `Get(key, default)` adds the key with the default value when it is missing, and the next save writes it to `config.json`. Prefix your own keys with the plugin name if you store settings there.

## State in memory

Each component is instantiated once per load, and static fields live as long as the plugin is loaded. Everything in memory is lost on reload or restart; persist what must survive.

Static fields are shared by every server the bot is in. Key your data by guild ID when it is per server.
