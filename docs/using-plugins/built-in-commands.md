# Built-in commands

## Calling a command

A prefix command is a message that starts with the **prefix** (`;` by default) or with a **mention** of the bot:

```text
;level
@Seth level
;help level
```

- The command name is the first word after the prefix or mention. Names and aliases are **case-sensitive**: `;level` works, `;Level` doesn't.
- Everything after the first word is passed to the command as arguments, split on spaces.
- Messages from bots (including Seth itself) are ignored.
- Commands marked *admin only* are silently ignored for users without the **Administrator** permission in that server.
- Commands also work in a direct message with the bot, if the plugin implements DM handling. Admin-only commands never run in DMs.

Every command run is logged in the console: `User (name) from Guild "server" executed command "level"`, or `User (name) from DM executed command "level"`.

Slash commands are different: Discord shows them in its `/` menu, with their own options. See [Slash commands](../creating-plugins/slash-commands.md).

## `help`

The only command built into the bot. It's loaded together with the plugins, so it is available after **Load plugins** even when no plugin is installed.

| Usage | Answer |
| --- | --- |
| `;help` | An embed with two fields: *Admin Commands* and *Normal Commands*. |
| `;help <command>` | An embed with the command's description, usage and aliases. The lookup ignores case. Aliases aren't matched here, only the main name. |

`help` only answers in servers; in a DM it does nothing.
