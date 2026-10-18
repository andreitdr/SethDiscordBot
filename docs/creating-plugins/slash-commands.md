# Slash commands and interactions (`IDbSlashCommand`)

Slash commands appear in Discord's `/` menu with typed options, validation and autocomplete of option names. A slash command plugin can also handle **interactions**: clicks on buttons and selections in menus it sent.

## The interface

```csharp
namespace DiscordBotCore.PluginCore.Interfaces;

public interface IDbSlashCommand
{
    string Name { get; }                              // "remind": lowercase, 1-32 characters, no spaces
    string Description { get; }                       // 1-100 characters
    bool CanUseDm { get; }                            // also available in DMs with the bot
    bool HasInteraction { get; }                      // receive component interactions in ExecuteInteraction
    List<SlashCommandOptionBuilder> Options { get; }  // up to 25 options; [] for none

    void ExecuteServer(ILogger logger, SocketSlashCommand context) { }
    void ExecuteDm(ILogger logger, SocketSlashCommand context) { }
    Task ExecuteInteraction(ILogger logger, SocketInteraction interaction) => Task.CompletedTask;
}
```

`ExecuteServer` runs when the command is used in a server, `ExecuteDm` when it's used in a DM (only possible with `CanUseDm = true`).

## Registration

When plugins load, each slash command is registered with Discord:

- **Per server**, for each ID in the `ServerIds` setting (the *Slash command servers* list in Settings). Changes show up immediately. Servers the bot isn't in are skipped with a warning.
- **Globally** when that list is empty. Global commands can take up to an hour to appear.

When plugins unload (reload, stop, delete), the registered commands are deleted from Discord again. Slash commands are only registered if the bot is in at least one server.

## Example: `/remind`

```csharp
using Discord;
using Discord.WebSocket;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Interfaces;

namespace ReminderPlugin;

public class RemindSlashCommand : IDbSlashCommand
{
    public string Name => "remind";
    public string Description => "Reminds you of something later";
    public bool CanUseDm => true;
    public bool HasInteraction => false;

    public List<SlashCommandOptionBuilder> Options =>
    [
        new SlashCommandOptionBuilder
        {
            Name = "minutes", Description = "In how many minutes",
            Type = ApplicationCommandOptionType.Integer, IsRequired = true, MinValue = 1, MaxValue = 1440
        },
        new SlashCommandOptionBuilder
        {
            Name = "text", Description = "What to remind you of",
            Type = ApplicationCommandOptionType.String, IsRequired = true
        }
    ];

    public async void ExecuteServer(ILogger logger, SocketSlashCommand context) => await Handle(logger, context);

    public async void ExecuteDm(ILogger logger, SocketSlashCommand context) => await Handle(logger, context);

    private static async Task Handle(ILogger logger, SocketSlashCommand context)
    {
        try
        {
            long minutes = (long)context.Data.Options.First(o => o.Name == "minutes").Value;
            string text = (string)context.Data.Options.First(o => o.Name == "text").Value;

            await context.RespondAsync($"⏰ I'll remind you in {minutes} min.", ephemeral: true);

            await Task.Delay(TimeSpan.FromMinutes(minutes));
            await context.User.SendMessageAsync($"⏰ Reminder: {text}");
        }
        catch (Exception ex)
        {
            logger.LogException(ex, typeof(RemindSlashCommand));
        }
    }
}
```

### Reading options

`context.Data.Options` holds the options the user filled in. Optional options that were left empty are missing, so use `FirstOrDefault` for them. The `Value` type depends on the option type:

| `ApplicationCommandOptionType` | `Value` type |
| --- | --- |
| `String` | `string` |
| `Integer` | `long` |
| `Number` | `double` |
| `Boolean` | `bool` |
| `User` | `SocketUser` / `SocketGuildUser` |
| `Channel` | `SocketChannel` subtype |
| `Role` | `SocketRole` |
| `Attachment` | `Attachment` |

### Responding

- Discord requires a response within **3 seconds**. Call `RespondAsync(...)` right away, or `DeferAsync()` and later `FollowupAsync(...)` for slow work.
- `ephemeral: true` makes the answer visible only to the user.
- Respond only once; use `FollowupAsync` or `ModifyOriginalResponseAsync` afterwards.

### `async void`

The `Execute…` methods return `void`, so they're usually written `async void`. An exception that escapes an `async void` method isn't caught by the bot and can crash the process: wrap the body in `try`/`catch` as above.

## Interactions: buttons and menus

Set `HasInteraction => true` and implement `ExecuteInteraction` to handle components (buttons, select menus) that your command sent.

`ExecuteInteraction` receives **every** interaction the bot gets: other plugins' buttons and every slash command too. Always filter, typically by a custom id prefix that belongs to your plugin:

```csharp
public bool HasInteraction => true;

public async void ExecuteServer(ILogger logger, SocketSlashCommand context)
{
    var buttons = new ComponentBuilder()
        .WithButton("Yes", "vote:yes", ButtonStyle.Success)
        .WithButton("No", "vote:no", ButtonStyle.Danger);

    await context.RespondAsync("Do you like pizza?", components: buttons.Build());
}

public async Task ExecuteInteraction(ILogger logger, SocketInteraction interaction)
{
    if (interaction is not SocketMessageComponent button || !button.Data.CustomId.StartsWith("vote:"))
        return;

    string answer = button.Data.CustomId["vote:".Length..];
    await button.RespondAsync($"You voted **{answer}**", ephemeral: true);
}
```

`Plugins/PollMaker` is a complete example: it encodes the poll and answer in the custom id (`poll:{id}:{index}`), keeps the votes in a static dictionary and edits the message on each click.

The interaction handler is detached when plugins unload. Components on old messages still exist in Discord, so handle "unknown" ids gracefully (PollMaker answers *This poll is no longer active*).

## Things to know

- Slash command names must be unique across plugins. If two plugins register the same name, the later registration replaces the earlier one in Discord, and calls go to whichever the bot finds first.
- `Options` is read when the command is registered, i.e. at load time.
- Registering many commands globally is rate-limited by Discord; during development, put your test server's ID in **Slash command servers**.

---

Next: [Storage, configuration and logging](storage-and-logging.md)
