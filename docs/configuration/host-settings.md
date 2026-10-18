# Host settings

The WebUI is a standard ASP.NET Core app, so besides the bot's [`config.json`](bot-configuration.md) it reads the usual ASP.NET Core configuration: `WebUI/appsettings.json`, `appsettings.{Environment}.json`, environment variables and command-line arguments.

## Settings read by the bot host

| Setting | Default | Meaning |
| --- | --- | --- |
| `ConfigFile` | `./Data/Resources/config.json` | Path of the bot configuration file. Its folder is created if needed. |
| `Logger:LogFolder` | `./Data/Logs` | Folder for daily log files. |
| `Logger:LogFormat` | `{ThrowTime} {SenderName} {Message}` | Format of each line in the log files and the process console. |
| `Logger:MaxHistorySize` | `1000` | How many log messages are kept in memory for the dashboard's Console panel. |
| `Dashboard:Password` | *(none)* | Password for the dashboard sign-in page. See [Dashboard password](#dashboard-password). |
| `Dashboard:AllowRemoteWithoutPassword` | `false` | Without a password, also answer requests from other machines. Only for a reverse proxy that does its own authentication. |

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

## Dashboard password

Set `Dashboard:Password`, usually as the `Dashboard__Password` environment variable, and restart the WebUI:

```bash
export Dashboard__Password='a long random password'
```

```bash
dotnet run --project WebUI
```

Every page, the live dashboard connection and plugin pages then need a sign-in at `/login`. Sign-in lasts 14 days (renewed while you use it) and survives restarts: the keys that encrypt the cookie are kept in `Data/Keys`. Changing the password signs everyone out. The **Sign out** button is at the right of the header. A browser gets 5 password attempts per minute.

Without a password, the dashboard only answers requests from the machine it runs on (`localhost`). Other machines get *403 Forbidden* with a short explanation.

## Exposing the dashboard

Whoever can use the dashboard can:

- read the console output,
- replace the bot token,
- start and stop the bot,
- upload a plugin DLL, which is arbitrary code that runs inside the host process.

So:

- Set a [dashboard password](#dashboard-password) before listening on anything but `localhost`.
- Serve it over HTTPS when it crosses a network you don't fully trust: the password and the sign-in cookie are only as private as the connection. Put it behind a reverse proxy (nginx, Caddy, Traefik, …) that terminates TLS. Blazor Server uses WebSockets, so the proxy must forward the `Upgrade`/`Connection` headers.
- Or keep it on the loopback address and use an SSH tunnel (`ssh -L 5028:127.0.0.1:5028 user@server`).
- If your reverse proxy already authenticates users and you don't want a second password, set `Dashboard__AllowRemoteWithoutPassword=true`. Then make sure nothing but the proxy can reach the port.
