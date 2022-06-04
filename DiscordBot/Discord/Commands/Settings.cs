using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using DiscordBot.Discord.Core;
using PluginManager.Interfaces;
using PluginManager.Others;
using PluginManager.Others.Permissions;

namespace DiscordBot.Discord.Commands
{
    class Settings : DBCommand
    {

        /// <summary>
        /// Command name
        /// </summary>
        public string Command => "set";

        /// <summary>
        /// Command Description
        /// </summary>
        public string Description => "This command allows you change all settings. Use \"set help\" to show details";

        /// <summary>
        /// Command usage
        /// </summary>
        public string Usage => "set [keyword] [new Value]";

        /// <summary>
        /// Check if the command can be used <inheritdoca DM <see cref="IChannel"/>/>
        /// </summary>
        public bool canUseDM => true;

        /// <summary>
        /// Check if the command can be used in a server
        /// </summary>
        public bool canUseServer => true;

        /// <summary>
        /// Check if the command require administrator to be executed
        /// </summary>
        public bool requireAdmin => true;

        /// <summary>
        /// The main body of the command
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="message">The command message</param>
        /// <param name="client">The discord bot client</param>
        /// <param name="isDM">True if the message was sent from a DM channel, false otherwise</param>
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
