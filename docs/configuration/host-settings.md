# Host settings

The WebUI is a standard ASP.NET Core app, so besides the bot's [`config.json`](bot-configuration.md) it reads the usual ASP.NET Core configuration: `WebUI/appsettings.json`, `appsettings.{Environment}.json`, environment variables and command-line arguments.

## Settings read by the bot host

| Setting | Default | Meaning |
| --- | --- | --- |
| `ConfigFile` | `./Data/Resources/config.json` | Path of the bot configuration file. Its folder is created if needed. |
| `Logger:LogFolder` | `./Data/Logs` | Folder for daily log files. |
| `Logger:LogFormat` | `{ThrowTime} {SenderName} {Message}` | Format of each line in the log files and the process console. |
| `Logger:MaxHistorySize` | `1000` | How many log messages are kept in memory for the dashboard's Console panel. |

`LogFormat` placeholders are the properties of a log message: `{ThrowTime}`, `{SenderName}`, `{Message}` and `{LogMessageType}`.

### Setting them

In `WebUI/appsettings.json`:

```json
{
  "ConfigFile": "/srv/seth/config.json",
  "Logger": {
    "LogFolder": "/var/log/seth",
    "LogFormat": "{ThrowTime} [{LogMessageType}] {SenderName}: {Message}",
    "MaxHistorySize": "2000"
  }
}
```

As environment variables (use `__` for the `:` separator):

```bash
export Logger__LogFolder=/var/log/seth
```

On the command line:

```bash
dotnet run --project WebUI -- --ConfigFile=/srv/seth/config.json
```

## Address and port

| How you run it | Default address | Change it with |
| --- | --- | --- |
| `dotnet run --project WebUI` | `http://localhost:5028` | `WebUI/Properties/launchSettings.json`, or `--urls` |
| `dotnet WebUI.dll` (published) | `http://localhost:5000` | `ASPNETCORE_URLS` or `--urls` |
| Docker | `http://+:8080` | `ASPNETCORE_HTTP_PORTS` / `ports:` in `docker-compose.yml` |

Example:

```bash
dotnet WebUI.dll --urls http://127.0.0.1:5028
```

## Environment

`ASPNETCORE_ENVIRONMENT=Development` (set by the launch profiles) shows detailed errors. Without it, the app runs as *Production*: errors go to the generic error page and HSTS is enabled.

## Exposing the dashboard

The dashboard has **no authentication**. Whoever can load it can:

- read the console output,
- replace the bot token,
- start and stop the bot,
- upload a plugin DLL, which is arbitrary code that runs inside the host process.

So:

- Bind to the loopback address (`--urls http://127.0.0.1:5028`) on any machine reachable from a network you don't fully trust.
- To manage a remote server, use an SSH tunnel (`ssh -L 5028:127.0.0.1:5028 user@server`), or
- put it behind a reverse proxy (nginx, Caddy, Traefik, …) that enforces authentication and HTTPS. Blazor Server uses WebSockets, so the proxy must forward the `Upgrade`/`Connection` headers.
