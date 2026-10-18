# Sample plugins

The `Plugins/` folder has four plugins. They are useful as they are and as examples to copy from when [writing your own](../creating-plugins/README.md).

| Plugin | Shows | Commands |
| --- | --- | --- |
| [LevelingSystem](#levelingsystem) | A command and an event sharing a SQLite database | `;level` (`lvl`, `rank`) |
| [PollMaker](#pollmaker) | A slash command with buttons (interactions) | `/make-poll` |
| [WebPagesDemo](#webpagesdemo) | Plugin web pages in the dashboard | `;webdemo` |
| [CppModuleDemo](#cppmoduledemo) | Calling a C++ library, and C++ calling back | `/cpp-module-demo`, `/stop-cpp-module-demo` |

## Building and adding a sample

```bash
dotnet build Plugins/PollMaker
```

The DLL is at `Plugins/PollMaker/bin/Debug/net10.0/PollMaker.dll`. Add it in the dashboard with **Installed plugins → Add local** (name `PollMaker`, any version), then **Load plugins** or **Reload plugins**. `dotnet build` at the repository root builds all of them.

## LevelingSystem

Users earn experience by chatting and level up.

- **Event** `Leveling System Event Handler`: on load, opens `Data/Resources/LevelingSystem/Users.db` and creates a `Settings.txt` (JSON) with defaults. On every non-command message from a user, it adds a random amount of EXP, at most once per cooldown, and announces level-ups in the channel.
- **Command** `;level` (aliases `lvl`, `rank`): shows the caller's level, EXP and the EXP needed for the next level.

`Data/Resources/LevelingSystem/Settings.txt`:

```json
{
  "SecondsToWaitBetweenMessages": 5,
  "MinExp": 1,
  "MaxExp": 7
}
```

Edit it and restart the bot (Stop, Start, Load plugins) to apply.

## PollMaker

`/make-poll` creates a poll with numbered vote buttons. Server channels only.

| Option | Required | Meaning |
| --- | --- | --- |
| `question` | yes | The poll question. |
| `answers` | yes | 2 to 25 answers separated by `;`, e.g. `Pizza; Pasta; Salad`. Duplicates are removed. |
| `timed` | no | `true` closes the poll automatically. |
| `duration` | if `timed` | Hours until it closes, 1 to 168. |

Click a button to vote, click it again to remove your vote. The counts update in the message. Polls are kept in memory: after a reload or restart, the buttons of old polls answer *This poll is no longer active*.

## WebPagesDemo

A plugin with pages in the WebUI, and a `;webdemo` command that points to them.

After loading plugins, open **Pages → Overview** in the header, or the links in the **Plugin pages** panel:

- **Overview** (`/`): reads the bot prefix from the host configuration, has an interactive button, and links to the other pages.
- **About** (`/about`): explains how pages and embedded assets work.
- **Item** (`/items/{id:int}`): a page with a typed route parameter. `items/not-a-number` doesn't match the `int` constraint and shows *Page not found*.
- `Pages/ConflictingItem.razor` has its attribute commented out. Uncomment it and reload plugins to see an overlapping route being rejected in the **Plugin pages** panel.

Its `.csproj` is the reference for building plugins with pages. See [Plugin web pages](../creating-plugins/web-pages.md).

## CppModuleDemo

A plugin that loads a native C++ library, calls its functions and passes it a C# callback. It needs some manual setup:

1. Build the native library:

   ```bash
   cmake -S Plugins/CppModuleDemo/Native -B Plugins/CppModuleDemo/Native/build && cmake --build Plugins/CppModuleDemo/Native/build
   ```

   This produces `libCppModuleDemo.dylib` (macOS), `libCppModuleDemo.so` (Linux) or `CppModuleDemo.dll` (Windows, in a configuration sub-folder such as `Debug/`).

2. Copy it into the plugin's data folder, `Data/Resources/CppModuleDemo/` (create it if needed).

3. Build the plugin (`dotnet build Plugins/CppModuleDemo`) and copy `CppCompatibilityModule.dll` from `Plugins/CppModuleDemo/bin/Debug/net10.0/` into `<WebUI binaries>/Libraries/CppModuleDemo/` (for `dotnet run`: `WebUI/bin/Debug/net10.0/Libraries/CppModuleDemo/`). The host doesn't ship that assembly; see [Dependencies](../creating-plugins/dependencies.md).

4. Add `CppModuleDemo.dll` with **Add local** and load plugins.

Commands:

- `/cpp-module-demo example-integer-value example-number-value example-string-value`: sends the values to the C++ function `modifyComplexObject`, which changes them, then asks C++ to call the C# callback, and replies with the results.
- `/stop-cpp-module-demo`: frees the native library.

See [Native libraries](../creating-plugins/native-libraries.md) for how it works.
