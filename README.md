# Seth Discord Bot

A Discord bot written in C# whose features come from **plugins**.
The bot connects to Discord, dispatches commands and hosts plugins; everything else (commands, event handlers, slash commands, even pages in its web dashboard) is added by plugin DLLs that you can install, enable, disable and reload at runtime.

![The WebUI dashboard](docs/images/dashboard-overview.jpg)

Built with:

- [.NET 10 (C#)](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Discord.Net](https://github.com/discord-net/Discord.Net)
- [ASP.NET Core Blazor](https://learn.microsoft.com/aspnet/core/blazor/) and [Radzen Blazor](https://blazor.radzen.com/) for the dashboard

## Features

- **Web dashboard**: start and stop the bot, watch the live console, edit settings, see latency and servers. Panels can be moved, resized, collapsed and hidden; light and dark themes.
- **Plugins** with three kinds of components: prefix **commands** (`;level`), **events** (react to anything the Discord client exposes) and **slash commands** with buttons and menus.
- **Hot reload**: add a DLL from the browser, enable or disable plugins and reload them without restarting the app.
- **Plugin web pages**: plugins can ship Razor pages that appear in the dashboard.
- **Marketplace**: install plugins and their dependencies from a plugin repository server.
- **Native code**: plugins can call C/C++ libraries.

## Quick start

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and a bot token from the [Discord Developer Portal](https://discord.com/developers/applications) with the *Presence*, *Server Members* and *Message Content* intents enabled ([step by step](docs/getting-started/create-discord-application.md)).

```bash
git clone https://github.com/andreitdr/SethDiscordBot
```

```bash
cd SethDiscordBot && dotnet run --project WebUI
```

Open <http://localhost:5028>, then:

1. **Settings**: paste the bot token, check the prefix (`;`), optionally add your server ID for instant slash commands, **Save**.
2. **Start** the bot.
3. **Load plugins**.
4. In Discord, type `;help`.

The full walkthrough is in [Getting started](docs/getting-started/requirements.md). To run in a container, see [Run with Docker](docs/getting-started/run-with-docker.md).

> The dashboard has no login. Keep it on `localhost` or behind an authenticating reverse proxy; see [Exposing the dashboard](docs/configuration/host-settings.md#exposing-the-dashboard).

## Writing a plugin

A plugin is a class library that references `DiscordBotCore.PluginCore` and implements `IDbCommand`, `IDbEvent` or `IDbSlashCommand`:

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
        await args.Context.Channel.SendMessageAsync($"Pong! {args.Context.Client.Latency} ms");
    }
}
```

Build it, add the DLL in the dashboard with **Installed plugins → Add local**, and reload plugins. See [Creating plugins](docs/creating-plugins/README.md) for commands, events, slash commands, storage, web pages, dependencies and native libraries.

## Sample plugins

| Plugin | What it does |
| --- | --- |
| [LevelingSystem](Plugins/LevelingSystem) | Experience and levels for chatting, `;level` |
| [PollMaker](Plugins/PollMaker) | `/make-poll` with vote buttons and an optional timer |
| [WebPagesDemo](Plugins/WebPagesDemo) | Pages in the dashboard, with routes, parameters and assets |
| [CppModuleDemo](Plugins/CppModuleDemo) | Calling a C++ library and receiving callbacks |

How to build and use them: [Sample plugins](docs/using-plugins/sample-plugins.md).

## Documentation

All documentation is in [`docs/`](docs/README.md):

- [Getting started](docs/getting-started/requirements.md): requirements, Discord application, running from source or Docker, first start
- [Configuration](docs/configuration/bot-configuration.md): `config.json`, host settings, the `Data` folder
- [Dashboard](docs/dashboard/overview.md): layout and panels
- [Using plugins](docs/using-plugins/managing-plugins.md): installing, enabling, reloading, the built-in `help` command, the marketplace
- [Creating plugins](docs/creating-plugins/README.md)
- [Architecture](docs/architecture/overview.md): projects, plugin lifecycle, working on the bot itself
- [Troubleshooting](docs/troubleshooting.md)

## Building and testing

```bash
dotnet build
```

```bash
dotnet test
```

## License

See [LICENSE.txt](LICENSE.txt).
