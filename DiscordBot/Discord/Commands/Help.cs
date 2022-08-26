using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Others;

namespace DiscordBot.Discord.Commands;

/// <summary>
///     The help command
/// </summary>
internal class Help : DBCommand
{
    /// <summary>
    ///     Command name
    /// </summary>
    public string Command => "help";

    public List<string> Aliases => null;

    /// <summary>
    ///     Command Description
    /// </summary>
    public string Description => "This command allows you to check all loaded commands";

    /// <summary>
    ///     Command usage
    /// </summary>
    public string Usage => "help <command>";

    /// <summary>
    ///     Check if the command can be used <inheritdoca DM <see cref="IChannel" />/>
    /// </summary>
    public bool canUseDM => true;

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
    public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        var args = Functions.GetArguments(message);
        if (args.Count != 0)
        {
            foreach (var item in args)
            {
                var e = GenerateHelpCommand(item);
                if (e is null)
                    context.Channel.SendMessageAsync("Unknown Command " + item);
                else
                    context.Channel.SendMessageAsync(embed: e.Build());
            }

            return;
        }

        var embedBuilder = new EmbedBuilder();

        var adminCommands  = "";
        var normalCommands = "";
        var DMCommands     = "";

        foreach (var cmd in PluginLoader.Commands!)
        {
            if (cmd.canUseDM)
                DMCommands += cmd.Command + " ";
            if (cmd.requireAdmin)
                adminCommands += cmd.Command + " ";
            if (cmd.canUseServer)
                normalCommands += cmd.Command + " ";
        }

        embedBuilder.AddField("Admin Commands", adminCommands);
        embedBuilder.AddField("Normal Commands", normalCommands);
        embedBuilder.AddField("DM Commands", DMCommands);
        context.Channel.SendMessageAsync(embed: embedBuilder.Build());
    }

    private EmbedBuilder GenerateHelpCommand(string command)
    {
        var embedBuilder = new EmbedBuilder();
        var cmd          = PluginLoader.Commands!.Find(p => p.Command == command || (p.Aliases is not null && p.Aliases.Contains(command)));
        if (cmd == null) return null;

        embedBuilder.AddField("Usage", cmd.Usage);
        embedBuilder.AddField("Description", cmd.Description);
        if (cmd.Aliases is null)
            return embedBuilder;
        embedBuilder.AddField("Alias", cmd.Aliases.Count == 0 ? "-" : string.Join(", ", cmd.Aliases));

        return embedBuilder;
    }
}
