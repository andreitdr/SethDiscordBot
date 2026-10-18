# Events (`IDbEvent`)

An event is a plugin component that runs code when plugins load. It receives the Discord client, so it can subscribe to anything Discord.Net exposes (members joining, messages, reactions, voice state, …) or start its own background work.

## The interface

```csharp
namespace DiscordBotCore.PluginCore.Interfaces;

public interface IDbEvent
{
    string Name { get; }          // shown in the dashboard and the logs
    string Description { get; }
    void Start(IDbEventExecutingArgument args);
}
```

`Start` is called **once per load**, after the bot has logged in and is ready, before commands and slash commands are registered.

## The arguments

`IDbEventExecutingArgument` (namespace `DiscordBotCore.PluginCore.Helpers.Execution.DbEvent`):

| Property | Type | Meaning |
| --- | --- | --- |
| `Client` | `DiscordSocketClient` | The connected client. Subscribe to its events here. |
| `BotPrefix` | `string` | The command prefix at load time, e.g. to ignore command messages. |
| `Logger` | `ILogger` | The bot logger. |
| `PluginBaseDirectory` | `DirectoryInfo` | `Data/Resources/<plugin assembly name>`, created before `Start` is called. Shared by all events of the plugin. |

## Example: welcome new members

```csharp
using Discord.WebSocket;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;
using DiscordBotCore.PluginCore.Interfaces;

namespace WelcomePlugin;

public class WelcomeEvent : IDbEvent
{
    public string Name => "Welcome";
    public string Description => "Sends a DM to new members";

    private ILogger _Logger = null!;

    public void Start(IDbEventExecutingArgument args)
    {
        _Logger = args.Logger;
        args.Client.UserJoined += OnUserJoined;
        _Logger.Log("Welcome event started", this);
    }

    private async Task OnUserJoined(SocketGuildUser user)
    {
        try
        {
            var dm = await user.CreateDMChannelAsync();
            await dm.SendMessageAsync($"Welcome to {user.Guild.Name}, {user.DisplayName}!");
        }
        catch (Exception ex)
        {
            // Users can block DMs from server members; don't let that crash the handler.
            _Logger.Log($"Couldn't DM {user.Username}: {ex.Message}", this, LogType.Warning);
        }
    }
}
```

`UserJoined` needs the *Server Members* privileged intent, which the bot already requests ([setup](../getting-started/create-discord-application.md#3-enable-the-privileged-gateway-intents)).

## Example: react to messages

```csharp
public void Start(IDbEventExecutingArgument args)
{
    string prefix = args.BotPrefix;

    args.Client.MessageReceived += async message =>
    {
        if (message.Author.IsBot || message.Content.StartsWith(prefix))
            return;

        if (message.Content.Contains("good bot", StringComparison.OrdinalIgnoreCase))
            await message.AddReactionAsync(new Discord.Emoji("❤️"));
    };
}
```

## Doing async work in `Start`

`Start` returns `void`. If you need to `await` during startup (opening a database, reading a file), either:

- keep `Start` short and do the async part inside the handlers, or
- use `async void Start(...)` like the LevelingSystem sample, and wrap the whole body in `try`/`catch`. An exception escaping an `async void` method is not caught by the loader and can bring down the process.

Exceptions thrown synchronously from `Start` are caught: the event is logged as failed and skipped.

## Unloading and reloading

There is no `Stop` method. When plugins are unloaded, the bot forgets the event object, but **handlers it attached to the client stay attached** until the bot stops (the client is disposed on **Stop**). So:

- after changing an event plugin, restart the bot (**Stop**, **Start**, **Load plugins**) instead of **Reload plugins**, or the old handlers keep running alongside the new ones;
- timers and background tasks you start also keep running. Make them check a cancellation flag, or tie them to the client's lifetime (for example stop when `Client.LoginState` is no longer `LoggedIn`).

## Things to know

- Discord.Net runs event handlers on the gateway task. A handler that takes more than a few seconds triggers *A … handler is blocking the gateway task* warnings. Keep handlers fast, or hand long work to `Task.Run`.
- Every event of every plugin gets the same client: filter by guild or channel if your plugin should only act in some places.
- To share state between an event and a command of the same plugin, use a static class (see `Plugins/LevelingSystem/Settings.cs`).

---

Next: [Slash commands](slash-commands.md)
