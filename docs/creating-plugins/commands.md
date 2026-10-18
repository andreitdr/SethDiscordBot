# Commands (`IDbCommand`)

A command answers a message that starts with the bot prefix (or a mention of the bot) followed by the command's name or one of its aliases, e.g. `;roll 2d6`.

## The interface

```csharp
namespace DiscordBotCore.PluginCore.Interfaces;

public interface IDbCommand
{
    string Command { get; }          // the name, case-sensitive: "roll"
    List<string> Aliases { get; }    // other names: ["r", "dice"]
    string Description { get; }      // shown by ;help <command>
    string Usage { get; }            // shown by ;help <command>: "roll <count>d<sides>"
    bool RequireAdmin { get; }       // true: only members with the Administrator permission

    Task ExecuteServer(IDbCommandExecutingArgument args) => Task.CompletedTask; // in a server channel
    Task ExecuteDm(IDbCommandExecutingArgument args) => Task.CompletedTask;     // in a DM
}
```

Implement `ExecuteServer`, `ExecuteDm` or both. The one you leave out does nothing. In `ExecuteDm`, `args.Context.Guild` is `null`.

## The arguments

`IDbCommandExecutingArgument` (namespace `DiscordBotCore.PluginCore.Helpers.Execution.DbCommand`):

| Property | Type | Content for `;roll 2d6 fast` |
| --- | --- | --- |
| `Context` | `SocketCommandContext` | Discord.Net context: `Client`, `Guild`, `Channel`, `User`, `Message`. |
| `CommandUsed` | `string` | `roll` (or the alias that was typed) |
| `Arguments` | `string[]?` | `["2d6", "fast"]`; **`null`** when nothing follows the command |
| `CleanContent` | `string` | `roll 2d6 fast` (the message without the prefix) |
| `Logger` | `ILogger` | The bot logger; messages appear in the dashboard console. |
| `PluginBaseDirectory` | `DirectoryInfo` | `Data/Resources/roll`, a folder for this command's files. **Not created for you**: call `Create()` before writing. |

Arguments are split on single spaces, so quoted text isn't kept together: rebuild it with `string.Join(' ', args.Arguments)` when you need the rest of the line.

## Example: a dice command

```csharp
using Discord;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
using DiscordBotCore.PluginCore.Interfaces;

namespace DicePlugin;

public class RollCommand : IDbCommand
{
    public string Command => "roll";
    public List<string> Aliases => ["r", "dice"];
    public string Description => "Rolls dice, e.g. 2d6";
    public string Usage => "roll <count>d<sides>";
    public bool RequireAdmin => false;

    public async Task ExecuteServer(IDbCommandExecutingArgument args)
    {
        if (args.Arguments is not [var spec, ..] || !TryParse(spec, out int count, out int sides))
        {
            await args.Context.Channel.SendMessageAsync($"Usage: `{Usage}`");
            return;
        }

        int[] rolls = Enumerable.Range(0, count).Select(_ => Random.Shared.Next(1, sides + 1)).ToArray();

        var embed = new EmbedBuilder()
            .WithTitle($"🎲 {count}d{sides}")
            .WithDescription($"{string.Join(" + ", rolls)} = **{rolls.Sum()}**")
            .WithAuthor(args.Context.User)
            .WithColor(Color.Green)
            .Build();

        await args.Context.Message.ReplyAsync(embed: embed);
        args.Logger.Log($"{args.Context.User.Username} rolled {count}d{sides}", this, LogType.Info);
    }

    private static bool TryParse(string spec, out int count, out int sides)
    {
        count = sides = 0;
        string[] parts = spec.Split('d');
        return parts.Length == 2
               && int.TryParse(parts[0], out count) && count is >= 1 and <= 100
               && int.TryParse(parts[1], out sides) && sides is >= 2 and <= 1000;
    }
}
```

## Admin-only commands

With `RequireAdmin => true` the command only runs for members who have the **Administrator** permission in that server; for everybody else the message is ignored without an answer. Admin-only commands never run in DMs, so their `ExecuteDm` is never called. `;help` lists such commands under *Admin Commands*.

For finer checks, inspect the member yourself:

```csharp
if (args.Context.User is SocketGuildUser { GuildPermissions.ManageMessages: true })
{
    // …
}
```

## Things to know

- **Names are case-sensitive** and must be unique across all plugins; the first loaded command with a matching name or alias wins. `help` is taken by the built-in command.
- Don't put the prefix in `Command`. The user's prefix is configurable.
- Each command type is instantiated once per load; instance fields are shared by all servers and users.
- Exceptions are caught and logged by the bot, but the user gets no answer, so handle expected errors and reply with something useful.
- Commands run on the gateway event thread. For slow work (HTTP calls, heavy computing) keep the `await` chain asynchronous; don't block with `.Result` or `Thread.Sleep`.

---

Next: [Events](events.md)
