# DiscordBotWithAPI

Plugin Types:
1. Commands
2. Events


## How to create a plugin
1. Commands <br>
First of all, Create a new project (class library) in Visual Studio.
![Imgur Image](https://i.imgur.com/KUqzKsB.png)
![Imgur Image](https://i.imgur.com/JzpEViR.png)
![Imgur Image](https://i.imgur.com/vtoEepX.png)
![Imgur Image](https://i.imgur.com/ceaVR2R.png)
![Imgur Image](https://i.imgur.com/UMSitk4.png)
![Imgur Image](https://i.imgur.com/GEjShdl.png)
Here is an example of class that is a command class
```cs
using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

namespace CMD_Utils
{
    class FlipCoin : DBCommand
    {
        public string Command => "flip";

        public string Description => "Flip a coin";

        public string Usage => "flip";

        public bool canUseDM => true;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            System.Random random = new System.Random();
            int r = random.Next(1, 3);
            if (r == 1)
                await message.Channel.SendMessageAsync("Heads");
            else await message.Channel.SendMessageAsync("Tails");
        }
    }
}

```
#### Definitions:
- Command - The keyword that triggers the execution for the command. This is what players must type in order to execute your command
- Description - The description of your command. Can be anything you like
- Usage - The usage of your command. This is what `help [Command]` command will display
- canUseDM - true if you plan to let users execute this command in DM chat with bot
- canUseServer - true if you plan to let the users execute this command in a server chat
- requireAdmin - true if this command requres an user with Administrator permission in the server
- Execute () - the function of your command.
  - context - the command context
  - message - the message itself
  - client - the discord bot client
  - isDM - true if the message was sent from DM chat

From here on, start coding. When your plugin is done, build it as any DLL project then add it to the following path
`{bot_executable}/Data/Plugins/Commands/<optional subfolder>/yourDLLName.dll`
Then, reload bot and execute command `lp` in bot's console. The plugin should be loaded into memory or an error is thrown if not. If an error is thrown, then
there is something wrong in your command's code.

2. Events
