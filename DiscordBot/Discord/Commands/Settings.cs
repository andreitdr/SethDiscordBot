using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Core;
using PluginManager.Interfaces;
using PluginManager.Others;
using PluginManager.Others.Permissions;

namespace DiscordBot.Discord.Commands
{
    class Settings : DBCommand
    {
        public string Command => "set";

        public string Description => "This command allows you change all settings. Use \"set help\" to show details";

        public string Usage => "set [keyword] [new Value]";

        public bool canUseDM => true;
        public bool canUseServer => true;
        public bool requireAdmin => true;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            var channel = message.Channel;
            try
            {

                string content = message.Content;
                string[] data = content.Split(' ');
                string keyword = data[1];
                if (keyword.ToLower() == "help")
                {
                    await channel.SendMessageAsync("set token [new value] -- set the value of the new token (require restart)");
                    await channel.SendMessageAsync("set prefix [new value] -- set the value of the new preifx (require restart)");

                    return;
                }

                switch (keyword.ToLower())
                {
                    case "token":
                        if (data.Length != 3)
                        {
                            await channel.SendMessageAsync("Invalid token !");
                            return;
                        }
                        Functions.WriteToSettings("./Data/Resources/DiscordBotCore.data", "BOT_TOKEN", data[2], '\t');
                        break;
                    case "prefix":
                        if (data.Length != 3)
                        {
                            await channel.SendMessageAsync("Invalid token !");
                            return;
                        }
                        Functions.WriteToSettings("./Data/Resources/DiscordBotCore.data", "BOT_PREFIX", data[2], '\t');
                        break;
                    default:
                        return;
                }

                await channel.SendMessageAsync("Restart required ...");
            }
            catch
            {
                await channel.SendMessageAsync("Unknown usage to this command !\nUsage: " + Usage);
            }

        }
    }
}
