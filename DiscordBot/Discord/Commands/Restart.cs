using System;
using System.Diagnostics;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Others;
using PluginManager.Others.Permissions;
using DiscordLibCommands = Discord.Commands;
using DiscordLib = Discord;
using OperatingSystem = PluginManager.Others.OperatingSystem;

namespace DiscordBot.Discord.Commands;

internal class Restart : DBCommand
{
    /// <summary>
    ///     Command name
    /// </summary>
    public string Command => "restart";

    /// <summary>
    ///     Command Description
    /// </summary>
    public string Description => "Restart the bot";

    /// <summary>
    ///     Command usage
    /// </summary>
    public string Usage => "restart [-p | -c | -args | -cmd] <args>";

    /// <summary>
    ///     Check if the command can be used <inheritdoca DM <see cref="IChannel" />/>
    /// </summary>
    public bool canUseDM => false;

    /// <summary>
    ///     Check if the command can be used in a server
    /// </summary>
    public bool canUseServer => true;

    /// <summary>
    ///     Check if the command require administrator to be executed
    /// </summary>
    public bool requireAdmin => false;

    /// <summary>
    ///     The main body of the command
    /// </summary>
    /// <param name="context">The command context</param>
    /// <param name="message">The command message</param>
    /// <param name="client">The discord bot client</param>
    /// <param name="isDM">True if the message was sent from a DM channel, false otherwise</param>
    public async void Execute(DiscordLibCommands.SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        if (!(message.Author as SocketGuildUser).hasPermission(DiscordLib.GuildPermission.Administrator)) return;
        var args = Functions.GetArguments(message);
        var OS   = Functions.GetOperatingSystem();
        if (args.Count == 0)
        {
            switch (OS)
            {
                case OperatingSystem.WINDOWS:
                    Process.Start("./DiscordBot.exe");
                    break;
                case OperatingSystem.LINUX:
                case OperatingSystem.MAC_OS:
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
                var cmd = "--args";

                if (args.Count > 1)
                    for (var i = 1; i < args.Count; i++)
                        cmd += $" {args[i]}";


                switch (OS)
                {
                    case OperatingSystem.WINDOWS:
                        Functions.WriteLogFile("Restarting the bot with the following arguments: \"" + cmd + "\"");
                        Process.Start("./DiscordBot.exe", cmd);
                        break;
                    case OperatingSystem.LINUX:
                        //case PluginManager.Others.OperatingSystem.MAC_OS: ?? - not tested
                        Process.Start("./DiscordBot", cmd);
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
