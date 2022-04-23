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
using PluginManager.Online;
using PluginManager.Others;

namespace DiscordBot.Discord.Commands
{
    class Restart : DBCommand
    {
        string DBCommand.Command => "restart";

        string DBCommand.Description => "Restart the bot";

        string DBCommand.Usage => "restart [-option]";

        bool DBCommand.canUseDM => false;

        bool DBCommand.canUseServer => true;

        bool DBCommand.requireAdmin => true;

        void DBCommand.Execute(dsc.SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
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


            }

        }
    }
}
