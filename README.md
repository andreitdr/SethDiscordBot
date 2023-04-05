# Seth Discord Bot

This is a Discord Bot made with C# that accepts plugins as extensions for more commands and events. All basic commands are built in already in the PluginManager class library. 
This project is based on:

- [.NET 6 (C#)](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)
- [Discord.Net](https://github.com/discord-net/Discord.Net)


## Plugins
#### Requirements:
- [Visual Studio](https://visualstudio.microsoft.com/thank-you-downloading-visual-studio/?sku=Community&channel=Release&version=VS2022&source=VSLandingPage&cid=2030&passive=false)
- .NET 6 (downloaded with Visual Studio)

Plugin Types:
1. Commands
2. Events
3. Slash Commands

### How to create a plugin

First of all, create a new project (class library) in Visual Studio.
Then import the PluginManager as reference to your project.

## 1. Commands

Commands are loaded when all plugins are loaded into memory. The Execute method is called whenever any user (that respects the `requireAdmin` propery) calls the command using the bot prefix and the `Command`.
Commands are plugins that allow users to interact with them. 
Here is an example:
```cs
using Discord;
using Discord.Commands;

using PluginManager.Interfaces;

namespace LevelingSystem;

internal class LevelCommand : DBCommand
{
    public string Command => "level";

    public List<string> Aliases => new() { "lvl" };

    public string Description => "Display tour current level";

    public string Usage => "level";

    public bool requireAdmin => false;

    public async void ExecuteServer(CmdArgs context)
    {
        object[] user = await Variables.database.ReadDataArrayAsync($"SELECT * FROM Levels WHERE UserID='{context.Message.Author.Id}'");
        if (user is null)
        {
            await context.Channel.SendMessageAsync("You are now unranked !");
            return;
        }

        int level = (int)user[1];
        int exp = (int)user[2];

        var builder = new EmbedBuilder();
        var r = new Random();
        builder.WithColor(r.Next(256), r.Next(256), r.Next(256));
        builder.AddField("Current Level", level, true)
               .AddField("Current EXP", exp, true)
               .AddField("Required Exp", (level * 8 + 24).ToString(), true);
        builder.WithTimestamp(DateTimeOffset.Now);
        builder.WithAuthor(context.Message.Author.Mention);
        await context.Channel.SendMessageAsync(embed: builder.Build());
    }

    //Optional method (tell the bot what should it do if the command is executed from a DM channel)
    //public async void ExecuteDM(CmdArgs context) {
    //
    //}
}


```
#### Code description:
- Command - The keyword that triggers the execution for the command. This is what players must type in order to execute your command
- Aliases - The aliases that can be used instead of the full name to execute the command
- Description - The description of your command. Can be anything you like
- Usage - The usage of your command. This is what `help [Command]` command will display
- requireAdmin - true if this command requres an user with Administrator permission in the server
- ExecuteServer () - the function that is executed only when the command is invoked in a server channel.  (optional)
  - context - the command context
- ExecuteDM () - the function that is executed only when the command is invoked in a private (DM) channel.  (optional)
  - context - the command context

From here on, start coding. When your plugin is done, build it as any DLL project then add it to the following path
`{bot_executable}/Data/Plugins/<optional subfolder>/[plugin name].dll`
Then, reload bot and execute command `lp` in bot's console. The plugin should be loaded into memory or an error is thrown if not. If an error is thrown, then
there is something wrong in your command's code.

## 2. Events

Events are loaded when all plugins are loaded. At the moment when they are loaded, the Start function is called.
Events are used if you want the bot to do something when something happens in server. The following example shows you how to catch when a user joins the server
and send to that user a DM message with `Welcome to server !`.

```cs
using PluginManager.Others;
using PluginManager.Interfaces;

public class OnUserJoin : DBEvent
{
    public string name => "MyEvent";

    public string description => "This is a demo event";

    public async void Start(Discord.WebSocket.DiscordSocketClient client)
    {
        client.UserJoined += async (user) => {
            await (await user.CreateDMChannelAsync()).SendMessageAsync("Welcome to server !");
        };
    }
}
```

#### Code description:
- name - The name of your event. It will appear in console when it loads
- description - The description of your event
- Start() - The main body of your event. This is executed when the bot loads all plugins
  - client - the discord bot client


## 3. Slash Commands


Slash commands are server based commands. They work the same way as normal commands, but they require the `/` prefix as they are integrated
with the UI of Discord.
Here is an example:
```cs
using Discord;
using Discord.WebSocket;

using PluginManager.Interfaces;

namespace SlashCommands
{
    public class Random : DBSlashCommand
    {
        public string Name => "random";

        public string Description => "Generates a random nunber between 2 values";

        public bool canUseDM => true;

        public List<SlashCommandOptionBuilder> Options => new List<SlashCommandOptionBuilder>()
        {
            new SlashCommandOptionBuilder() {Name = "min-value", Description = "Minimum value", IsRequired=true, Type = ApplicationCommandOptionType.Integer, MinValue = 0, MaxValue = int.MaxValue-1},
            new SlashCommandOptionBuilder() {Name="max-value", Description = "Maximum value", IsRequired=true, Type=ApplicationCommandOptionType.Integer,MinValue = 0, MaxValue = int.MaxValue-1}
        };

        public async void ExecuteServer(SocketSlashCommand command)
        {
            var rnd = new System.Random();
            var options = command.Data.Options.ToArray();
            if (options.Count() != 2)
            {
                await command.RespondAsync("Invalid parameters", ephemeral: true);
                return;
            }

            Int64 numberOne = (Int64)options[0].Value;
            Int64 numberTwo = (Int64)options[1].Value;

            await command.RespondAsync("Your generated number is " + rnd.Next((int)numberOne, (int)numberTwo), ephemeral: true);

        }
    }
}
```

#### Code description:
- Name - the command name (execute with /{Name})
- Description - The description of the command
- canUseDM - true id this command can be activated in DM chat, false otherwise
- Options - the arguments of the command
- ExecuteServer() - this function will be called if the command is invoked in a server channel  (optional)
  - context - the command context
- ExecuteDM() - this function will be called if the command is invoked in a DM channel  (optional)
  - context - the command context


## Note: 
You can create multiple commands, events and slash commands into one single plugin (class library). The PluginManager will detect the classes and load them individualy. If there are more commands (normal commands, events or slash commands) into a single project (class library) they can use the same resources (a class for example) that is contained within the plugin. 


> Updated: 5.04.2023