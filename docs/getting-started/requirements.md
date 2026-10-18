# Requirements

## To run the bot

| What | Why |
| --- | --- |
| [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | Every project targets `net10.0`. `global.json` pins SDK `10.0.0` with `rollForward: latestMajor`, so any newer SDK also works. |
| A Discord account | To create the bot application and its token. |
| A browser | The bot is controlled from a web dashboard. |
| Windows, Linux or macOS | All three work. The marketplace filters plugins by operating system. |

Or, instead of the SDK, [Docker](run-with-docker.md).

### Installing the .NET 10 SDK

**Windows**: download the installer from the [.NET download page](https://dotnet.microsoft.com/download/dotnet/10.0), or:

```bash
winget install Microsoft.DotNet.SDK.10
```

**macOS**: download the installer from the same page, or:

```bash
brew install --cask dotnet-sdk
```

**Linux**: follow [Install .NET on Linux](https://learn.microsoft.com/dotnet/core/install/linux) for your distribution. On Ubuntu, for example:

```bash
sudo apt-get update && sudo apt-get install -y dotnet-sdk-10.0
```

Check the installation:

```bash
dotnet --list-sdks
```

## To write plugins

Everything above, plus an editor that understands C# projects:

- [Visual Studio 2022/2026](https://visualstudio.microsoft.com/) with the *ASP.NET and web development* workload,
- [JetBrains Rider](https://www.jetbrains.com/rider/), or
- [VS Code](https://code.visualstudio.com/) with the C# Dev Kit extension.

Plugins that call native code also need a C/C++ toolchain and [CMake](https://cmake.org/) 3.20 or newer. See [Native libraries](../creating-plugins/native-libraries.md).

---

Next: [Create the Discord application](create-discord-application.md)
