using System.Collections.Generic;

using Discord;

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
    public void ExecuteServer(CmdArgs args)
    {
        if (args.arguments is not null)
        {
            var e = GenerateHelpCommand(args.arguments[0]);
            if (e is null)
                args.context.Channel.SendMessageAsync("Unknown Command " + args.arguments[0]);
            else
                args.context.Channel.SendMessageAsync(embed: e.Build());


            return;
        }

        var embedBuilder = new EmbedBuilder();

        var adminCommands = "";
        var normalCommands = "";

        foreach (var cmd in PluginLoader.Commands)
            if (cmd.requireAdmin)
                adminCommands += cmd.Command + " ";
            else
                normalCommands += cmd.Command + " ";


        if (adminCommands.Length > 0)
            embedBuilder.AddField("Admin Commands", adminCommands);
        if (normalCommands.Length > 0)
            embedBuilder.AddField("Normal Commands", normalCommands);
        args.context.Channel.SendMessageAsync(embed: embedBuilder.Build());
    }

    private EmbedBuilder GenerateHelpCommand(string command)
    {
        var embedBuilder = new EmbedBuilder();
        var cmd = PluginLoader.Commands.Find(p => p.Command == command ||
                                                   (p.Aliases is not null && p.Aliases.Contains(command)));
        if (cmd == null) return null;

        embedBuilder.AddField("Usage", Config.Data["prefix"] + cmd.Usage);
        embedBuilder.AddField("Description", cmd.Description);
        if (cmd.Aliases is null)
            return embedBuilder;
        embedBuilder.AddField("Alias", cmd.Aliases.Count == 0 ? "-" : string.Join(", ", cmd.Aliases));

        return embedBuilder;
    }
}