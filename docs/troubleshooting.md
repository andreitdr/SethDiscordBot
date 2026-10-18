# Troubleshooting

Start with the **Console** panel: most problems are explained there. The same messages are in `Data/Logs/<date>.log`.

## Setup

### `dotnet` complains about the SDK version

The projects need the .NET 10 SDK. Check with `dotnet --list-sdks` and install it ([Requirements](getting-started/requirements.md)).

### I can't find my configuration or plugins

The `Data` folder is relative to the working directory. With `dotnet run --project WebUI` it is `WebUI/Data`, not the repository root. See [The Data folder](configuration/data-folder.md).

### The dashboard isn't reachable from another computer

By default it listens on `localhost` only. Use `--urls http://0.0.0.0:5028` to listen on all interfaces, but read [Exposing the dashboard](configuration/host-settings.md#exposing-the-dashboard) first: there is no login.

## Connecting

### Start is disabled

No token is saved. Enter it in **Settings** and click **Save**.

### The bot stays on *Connecting…*

After 30 seconds the dashboard reports *The bot is taking long to connect*. Check, in this order:

1. **Privileged intents.** The bot requests all intents. Turn on *Presence*, *Server Members* and *Message Content* in the Developer Portal ([how](getting-started/create-discord-application.md#3-enable-the-privileged-gateway-intents)). Without them Discord closes the connection with *Disallowed intent(s)*, visible in the console.
2. **Token.** A rejected token shows *Discord rejected the token*; the bot also clears the saved token. Reset it in the Developer Portal and save the new one.
3. **Network.** The machine must reach `discord.com` and the gateway over HTTPS/WSS.

Click **Cancel** to stop retrying.

### The token disappears from Settings

When Discord answers 401 (invalid token), the bot deletes the token from `config.json` so it doesn't keep retrying with it. Save a valid token.

## Plugins

### *Load plugins* is disabled

Plugins can only be loaded while the bot is online. Start it first.

### My plugin isn't loaded

- Is it in **Installed plugins**, and **enabled**? Dropping a DLL into `Data/Plugins` isn't enough; use **Add local**.
- Did you **Load** or **Reload** plugins after adding or enabling it?
- Read the console lines from the load; [Build, test and debug](creating-plugins/build-test-debug.md#reading-load-errors) explains each message.
- Is the class non-abstract, with a parameterless constructor, implementing the interface from `DiscordBotCore.PluginCore`?

### *Some types in X could not be loaded*

A dependency of the plugin is missing. Put it in `Libraries/<assembly name>/` next to the WebUI binaries ([Dependencies](creating-plugins/dependencies.md)).

### My command doesn't answer

- Prefix and name are case-sensitive: `;Ping` isn't `;ping`.
- Check the prefix in **Settings**.
- The bot needs *Message Content* intent, and permission to read and send messages in that channel.
- `RequireAdmin` commands ignore non-administrators silently, and never run in DMs.
- `Arguments` is `null` when nothing follows the command; a `NullReferenceException` in the console usually means that.
- In a DM, only `ExecuteDm` runs; a command that implements just `ExecuteServer` does nothing there.
- The method signature must match the interface exactly, or the empty default implementation runs instead.

### My slash command doesn't show up

- Without **Slash command servers**, commands are global and can take up to an hour to appear. Add your server's ID for instant registration.
- The bot must be invited with the `applications.commands` scope.
- The bot must be in at least one server, and in each server listed.
- Look for `Failed to register slash command …` in the console: Discord rejects names with capitals or spaces, descriptions over 100 characters, more than 25 options, …

### *The application did not respond* in Discord

A slash command must respond within 3 seconds. Call `RespondAsync` first, or `DeferAsync` then `FollowupAsync` ([Slash commands](creating-plugins/slash-commands.md#responding)). Also check the console for an exception in your handler.

### Things happen twice after reloading

An event subscribed to the client again on reload while its old handler is still attached. Restart the bot (**Stop**, **Start**, **Load plugins**) after changing event plugins ([Events](creating-plugins/events.md#unloading-and-reloading)).

### The bot process crashed

Usually an exception in an `async void` method (an event's `Start` or a slash command's `Execute…`), or a crash in native code. Wrap `async void` bodies in `try`/`catch`.

### The Marketplace is empty or unavailable

- The marketplace needs a [plugin repository](using-plugins/marketplace.md) server. Without configuration it tries `http://localhost:8080/api/v1/`; *Connection refused* in the console means nothing is listening there.
- Only approved plugins for the current OS are listed.

## Plugin web pages

### The page says *This plugin isn't loaded*

Pages exist only while plugins are loaded. Start the bot and load plugins.

### My page isn't listed

- Pages need a `Title` to appear in menus, and pages with route parameters are never listed.
- Check the **Plugin pages** panel for a rejection and its reason ([Validation](creating-plugins/web-pages.md#validation)).
- Use `[PluginPage]`, not `@page`.

### Every page is rejected with a type identity error

The plugin ships its own `PluginPages.Abstractions.dll`. Reference it with `Private="false"` and remove the copy ([Web pages](creating-plugins/web-pages.md#project-file)).

### Links go to the wrong place

Build them with `Context.Href(...)`; plain relative links resolve from the site root.
