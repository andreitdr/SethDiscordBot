using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using System.Diagnostics;

using System.Text;
using System.Threading.Tasks;

using Discord.WebSocket;
using dsc = Discord.Commands;
using ds = Discord;

using PluginManager.Interfaces;
using PluginManager.Others.Permissions;
using PluginManager.Others;

namespace DiscordBot.Discord.Commands
{
    internal class Restart : DBCommand
    {
        /// <summary>
        /// Command name
        /// </summary>
        public string Command => "restart";

        /// <summary>
        /// Command Description
        /// </summary>
        public string Description => "Restart the bot";

        /// <summary>
        /// Command usage
        /// </summary>
        public string Usage => "restart [-p | -c | -args | -cmd] <args>";

        /// <summary>
        /// Check if the command can be used <inheritdoca DM <see cref="IChannel"/>/>
        /// </summary>
        public bool canUseDM => false;

        /// <summary>
        /// Check if the command can be used in a server
        /// </summary>
        public bool canUseServer => true;

        /// <summary>
        /// Check if the command require administrator to be executed
        /// </summary>
        public bool requireAdmin => false;
        /// <summary>
        /// The main body of the command
        /// </summary>
        /// <param name="context">The command context</param>
        /// <param name="message">The command message</param>
        /// <param name="client">The discord bot client</param>
        /// <param name="isDM">True if the message was sent from a DM channel, false otherwise</param>
        public async void Execute(dsc.SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            if (!DiscordPermissions.hasPermission(message.Author as SocketGuildUser, ds.GuildPermission.Administrator)) return;
            var args = Functions.GetArguments(message);
            var OS = Functions.GetOperatinSystem();
            if (args.Count == 0)
            {
                switch (OS)
                {
                    case PluginManager.Others.OperatingSystem.WINDOWS:
                        Process.Start("./DiscordBot.exe");
                        break;
                    case PluginManager.Others.OperatingSystem.LINUX:
                    case PluginManager.Others.OperatingSystem.MAC_OS:
                        Process.Start("./DiscordBot");
                        break;
                    default:
                        return;
                }
                return;
            }
            switch (args[0])
            {
                case "-p":
                case "-poweroff":
                case "-c":
                case "-close":
                    Environment.Exit(0);
                    break;
                case "-cmd":
                case "-args":

                    switch (OS)
                    {
                        case PluginManager.Others.OperatingSystem.WINDOWS:
                            Process.Start("./DiscordBot.exe", Functions.MergeStrings(args.ToArray(), 1));
                            break;
                        case PluginManager.Others.OperatingSystem.LINUX:
                        case PluginManager.Others.OperatingSystem.MAC_OS:
                            Process.Start("./DiscordBot", Functions.MergeStrings(args.ToArray(), 1));
                            break;
                        default:
                            return;
                    }
                    Environment.Exit(0);
                    break;
                default:
                    await context.Channel.SendMessageAsync("Invalid argument. Use `help restart` to see the usage.");
                    break;


            }

        }
    }
}
