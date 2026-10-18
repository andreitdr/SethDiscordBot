# Build, test and debug

## The development loop

1. Run the WebUI from your IDE or with `dotnet run --project WebUI`, start the bot and put your test server's ID in **Settings → Slash command servers** (slash commands then update instantly).
2. Build the plugin: `dotnet build Plugins/MyPlugin`.
3. First time: **Add local** with `Plugins/MyPlugin/bin/Debug/net10.0/MyPlugin.dll`.
4. Later builds: copy the new DLL over the one in `Data/Plugins` (it isn't locked) and click **Reload plugins**. The loader reads the file into memory on each load, so the new version is used.
5. Try it in Discord and watch the **Console** panel.

For plugins with events, restart the bot instead of reloading (see [Events](events.md#unloading-and-reloading)).

### Copy the DLL automatically

Add a post-build step to the plugin's `.csproj` so step 4 is just *build, then Reload plugins*:

```xml
<Target Name="CopyToBot" AfterTargets="Build">
  <Copy SourceFiles="$(TargetPath);$(TargetDir)$(TargetName).pdb"
        DestinationFolder="$(MSBuildThisFileDirectory)..\..\WebUI\Data\Plugins" />
</Target>
```

The file name must match the one the plugin was added with.

## Debugging

The plugin runs inside the WebUI process, so debug that process:

- **Visual Studio / Rider**: start the `WebUI` project with the debugger (or attach to the running `WebUI` process), set breakpoints in your plugin source and reload plugins.
- **VS Code**: attach the .NET debugger to the `WebUI` process.

Breakpoints bind because the loader also loads the `.pdb` next to the DLL. Copy the `.pdb` along with the DLL (the snippet above does).

## Reading load errors

The console tells you what happened during **Load plugins**:

| Message | Meaning |
| --- | --- |
| `Command loaded: x` / `Event loaded: x` / `Slash command loaded: x` | Fine. |
| `No enabled plugins` / `No plugins installed` | Nothing to load. |
| `Plugin X: file … does not exist.` | The database row points to a missing file. Re-add the plugin. |
| `Plugin X: failed to load …` | Not a valid .NET assembly, or built for another runtime. |
| `Some types in X could not be loaded: …` | A dependency is missing; see [Dependencies](dependencies.md). |
| `Failed to create instance of plugin type …` | The constructor threw, or there's no parameterless constructor. |
| `Event X failed to start: …` | `Start` threw. |
| `Skipping N slash command(s): the bot is not in any guild.` | Invite the bot to a server first. |
| `Failed to register slash command X: …` | Discord rejected the definition (invalid name, description length, too many options, …). |
| `Received slash command /x, but no loaded plugin handles it.` | A stale command is still registered in Discord; reload plugins, or wait for global changes to propagate. |

## Unit testing

Keep the logic in plain classes that don't depend on Discord, and test those with xUnit like the projects in `Tests/`. The execution-argument interfaces (`IDbCommandExecutingArgument`, `IDbEventExecutingArgument`) and `ILogger` are easy to mock with Moq; Discord.Net's socket types are not, so keep Discord calls in a thin layer.

```bash
dotnet test
```

## Before you share a plugin

- Build in Release: `dotnet build -c Release`.
- Bump the version (`major.minor.patch`) for each release.
- List the extra files it needs: DLLs for `Libraries/<plugin>/`, native libraries, data files.
- Test it on each operating system you claim to support.
