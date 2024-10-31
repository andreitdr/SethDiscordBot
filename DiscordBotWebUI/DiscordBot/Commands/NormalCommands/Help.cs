using System.Collections.Generic;
using Discord;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Loaders;
using DiscordBotCore.Others;

namespace DiscordBotWebUI.DiscordBot.Commands.NormalCommands;

/// <summary>
///     The help command
/// </summary>
internal class Help: IDbCommand
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
    public bool RequireAdmin => false;

    /// <summary>
    ///     The main body of the command
    /// </summary>
    /// <param name="context">The command context</param>
    public void ExecuteServer(DbCommandExecutingArguments args)
    {
        if (args.Arguments is not null)
        {
            var e = GenerateHelpCommand(args.Arguments[0]);
            if (e is null)
                args.Context.Channel.SendMessageAsync("Unknown Command " + args.Arguments[0]);
            else
                args.Context.Channel.SendMessageAsync(embed: e.Build());


            return;
        }

        var embedBuilder = new EmbedBuilder();

        var adminCommands  = "";
        var normalCommands = "";

        foreach (var cmd in PluginLoader.Commands)
            if (cmd.RequireAdmin)
                adminCommands += cmd.Command + " ";
            else
                normalCommands += cmd.Command + " ";


        if (adminCommands.Length > 0)
            embedBuilder.AddField("Admin Commands", adminCommands);
        if (normalCommands.Length > 0)
            embedBuilder.AddField("Normal Commands", normalCommands);
        args.Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
    }

    private EmbedBuilder GenerateHelpCommand(string command)
    {
        var embedBuilder = new EmbedBuilder();
        var cmd = PluginLoader.Commands.Find(p => p.Command == command ||
                                                  p.Aliases is not null && p.Aliases.Contains(command)
        );
        if (cmd == null) return null;

        string prefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("prefix");
        
        embedBuilder.AddField("Usage", prefix + cmd.Usage);
        embedBuilder.AddField("Description", cmd.Description);
        if (cmd.Aliases is null)
            return embedBuilder;
        embedBuilder.AddField("Alias", cmd.Aliases.Count == 0 ? "-" : string.Join(", ", cmd.Aliases));

        return embedBuilder;
    }
}
