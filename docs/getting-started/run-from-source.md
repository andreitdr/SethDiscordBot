# Run from source

The bot has no console program. It is hosted by the **WebUI** project, an ASP.NET Core (Blazor Server) app that starts the bot, loads plugins and serves the dashboard.

## Clone and build

```bash
git clone https://github.com/andreitdr/SethDiscordBot
cd SethDiscordBot
dotnet build
```

`dotnet build` builds the whole solution (`SethDiscordBot.sln`): the core libraries, the WebUI, the sample plugins and the tests.

## Start the WebUI

```bash
dotnet run --project WebUI
```

Then open <http://localhost:5028>. The address comes from the `http` profile in `WebUI/Properties/launchSettings.json`, which also sets `ASPNETCORE_ENVIRONMENT=Development`. To use HTTPS as well (`https://localhost:7127`), run with the `https` profile:

```bash
dotnet run --project WebUI --launch-profile https
```

### Where the data goes

All bot data (configuration, plugin database, plugin files, logs) is stored in a `Data` folder **relative to the working directory of the process**. `dotnet run --project WebUI` runs the app with `WebUI/` as its working directory, so the data ends up in `WebUI/Data/`. The folder is created on the first start and is ignored by git. See [The Data folder](../configuration/data-folder.md).

## Run a published build

For a server, publish a release build and run it from its own folder:

```bash
dotnet publish WebUI -c Release -o ./publish
```

```bash
cd publish && ASPNETCORE_URLS=http://localhost:5028 dotnet WebUI.dll
```

On Windows PowerShell, set the variable with `$env:ASPNETCORE_URLS="http://localhost:5028"` first, then run `dotnet WebUI.dll`.

The `Data` folder is then created inside `publish/`. Keep it when you update: publish the new version over the old files, or copy `Data` across.

To keep the bot running after you log out, run it as a service (systemd on Linux, a Windows service, launchd on macOS) or use [Docker](run-with-docker.md).

> **Security**: without a password the dashboard only answers `localhost`. Anyone who can use it can read logs, change the token and upload a plugin DLL, which is code that runs on your machine. To open it from other devices, set the `Dashboard__Password` environment variable and serve it over HTTPS. See [Dashboard password](../configuration/host-settings.md#dashboard-password).

---

Next: [First start](first-start.md)
