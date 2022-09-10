using System.Collections.Generic;
using System.Linq;

using Discord;
using Discord.Commands;
using Discord.WebSocket;

using PluginManager;
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
    ///     Check if the command require administrator to be executed
    /// </summary>
    public bool requireAdmin => false;

    /// <summary>
    ///     The main body of the command
    /// </summary>
    /// <param name="context">The command context</param>
    public void ExecuteServer(SocketCommandContext context)
    {
        var args = Functions.GetArguments(context.Message);
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

        var adminCommands = "";
        var normalCommands = "";

        foreach (var cmd in PluginLoader.Commands!)
        {
            if (cmd.requireAdmin)
                adminCommands += cmd.Command + " ";
            else
                normalCommands += cmd.Command + " ";
        }

        embedBuilder.AddField("Admin Commands", adminCommands);
        embedBuilder.AddField("Normal Commands", normalCommands);
        context.Channel.SendMessageAsync(embed: embedBuilder.Build());
    }

    private EmbedBuilder GenerateHelpCommand(string command)
    {
        var embedBuilder = new EmbedBuilder();
        var cmd = PluginLoader.Commands!.Find(p => p.Command == command || (p.Aliases is not null && p.Aliases.Contains(command)));
        if (cmd == null) return null;

        embedBuilder.AddField("Usage", Config.GetValue<string>("prefix") + cmd.Usage);
        embedBuilder.AddField("Description", cmd.Description);
        if (cmd.Aliases is null)
            return embedBuilder;
        embedBuilder.AddField("Alias", cmd.Aliases.Count == 0 ? "-" : string.Join(", ", cmd.Aliases));

        return embedBuilder;
    }
}
