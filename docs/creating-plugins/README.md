# Creating plugins

A plugin is a .NET 10 class library. The bot loads its DLL at runtime, finds every class that implements one of the plugin interfaces, creates one instance of each and wires it to Discord.

| Interface | For | Called |
| --- | --- | --- |
| [`IDbCommand`](commands.md) | Prefix commands, such as `;level` | When a message starts with the prefix (or a mention) followed by the command name or an alias |
| [`IDbEvent`](events.md) | Background behavior: reacting to joins, messages, reactions, timers… | `Start` once, when plugins load |
| [`IDbSlashCommand`](slash-commands.md) | Slash commands, such as `/make-poll`, and their buttons/menus | When the slash command (or an interaction) is used |

One plugin can contain any number of each, and they can share code and state (static classes, a shared database, …). It can also contain [web pages](web-pages.md) for the dashboard.

The interfaces live in **`DiscordBotCore.PluginCore`**. That is the only project a plugin needs to reference; it brings Discord.Net and the logger with it.

## Quick start: a `ping` command

1. Create the project inside the repository and reference the plugin contract:

   ```bash
   dotnet new classlib -n PingPlugin -o Plugins/PingPlugin -f net10.0
   ```

   ```bash
   dotnet add Plugins/PingPlugin reference DiscordBotCore.PluginCore/DiscordBotCore.PluginCore.csproj
   ```

   (Optionally mark the reference `Private="false"` in the `.csproj`; [Project setup](project-setup.md) explains why.)

2. Replace `Plugins/PingPlugin/Class1.cs` with `PingCommand.cs`:

   ```csharp
   using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
   using DiscordBotCore.PluginCore.Interfaces;

   namespace PingPlugin;

   public class PingCommand : IDbCommand
   {
       public string Command => "ping";
       public List<string> Aliases => ["p"];
       public string Description => "Checks that the bot answers";
       public string Usage => "ping";
       public bool RequireAdmin => false;

       public async Task ExecuteServer(IDbCommandExecutingArgument args)
       {
           int latency = args.Context.Client.Latency;
           await args.Context.Channel.SendMessageAsync($"Pong! Gateway latency: {latency} ms");
       }
   }
   ```

3. Build it:

   ```bash
   dotnet build Plugins/PingPlugin
   ```

4. In the dashboard: **Installed plugins → Add local**, pick `Plugins/PingPlugin/bin/Debug/net10.0/PingPlugin.dll`, name `PingPlugin`, version `1.0.0`. Start the bot if needed, then **Load plugins** (or **Reload plugins**).

5. In Discord: `;ping`.

## Rules for plugin types

- A class is picked up if it is **non-abstract** and implements `IDbCommand`, `IDbEvent` or `IDbSlashCommand`. It may be `public` or `internal`.
- It needs a **parameterless constructor**. The loader calls `Activator.CreateInstance`, so there's no dependency injection; everything a plugin needs from the bot arrives in method arguments.
- **One instance per load.** The same object handles every call until plugins are unloaded, so instance fields persist between calls (and must be thread-safe if you mutate them from handlers).
- Exceptions thrown from a constructor or `Start` are logged and that component is skipped; the rest of the plugin still loads.
- Method signatures must match the interface **exactly**. `Execute…` methods have default (empty) implementations in the interfaces, so a method with a slightly different signature compiles but is never called.

## Next

- [Project setup](project-setup.md): references, where to put the project, what (not) to ship.
- [Commands](commands.md), [Events](events.md), [Slash commands](slash-commands.md).
- [Storage, configuration and logging](storage-and-logging.md).
- [Web pages](web-pages.md), [Dependencies](dependencies.md), [Native libraries](native-libraries.md).
- [Build, test and debug](build-test-debug.md).
- [Plugin lifecycle](../architecture/plugin-lifecycle.md): exactly what happens on load and unload.
