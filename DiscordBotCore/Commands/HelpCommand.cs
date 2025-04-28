using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Discord;
using DiscordBotCore.Bot;
using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
using DiscordBotCore.PluginCore.Interfaces;

namespace DiscordBotCore.Commands;

public class HelpCommand : IDbCommand
{
    public string Command => "help";
    public List<string> Aliases => [];
    public string Description => "Help command for the bot.";
    public string Usage => "help <command>";
    public bool RequireAdmin => false;

    public async Task ExecuteServer(IDbCommandExecutingArgument args)
    {
        if (args.Arguments is not null)
        {
            string searchedCommand = args.Arguments[0];
            IDbCommand? command = DiscordBotApplication._InternalPluginLoader.Commands.FirstOrDefault(c => c.Command.Equals(searchedCommand, StringComparison.OrdinalIgnoreCase));
            
            if (command is null)
            {
                await args.Context.Channel.SendMessageAsync($"Command `{searchedCommand}` not found.");
                return;
            }
            
            EmbedBuilder helpEmbed = GenerateHelpCommand(command);
            await args.Context.Channel.SendMessageAsync(embed: helpEmbed.Build());
            return;
        }
        
        if (DiscordBotApplication._InternalPluginLoader.Commands.Count == 0)
        {
            await args.Context.Channel.SendMessageAsync("No commands found.");
            return;
        }
        
        var embedBuilder = new EmbedBuilder();

        var adminCommands  = "";
        var normalCommands = "";

        foreach (var cmd in DiscordBotApplication._InternalPluginLoader.Commands)
            if (cmd.RequireAdmin)
                adminCommands += cmd.Command + " ";
            else
                normalCommands += cmd.Command + " ";


        if (adminCommands.Length > 0)
            embedBuilder.AddField("Admin Commands", adminCommands);
        if (normalCommands.Length > 0)
            embedBuilder.AddField("Normal Commands", normalCommands);
        await args.Context.Channel.SendMessageAsync(embed: embedBuilder.Build());
    }
    
    private EmbedBuilder GenerateHelpCommand(IDbCommand command)
    {
        EmbedBuilder builder = new();
        builder.WithTitle($"Command: {command.Command}");
        builder.WithDescription(command.Description);
        builder.WithColor(Color.Blue);
        builder.AddField("Usage", command.Usage);
        string aliases = "";
        foreach (var alias in command.Aliases)
            aliases += alias + " ";
        builder.AddField("Aliases", aliases.Length > 0 ? aliases : "None");
        return builder;
    }
}