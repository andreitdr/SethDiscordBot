using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Others;

namespace DiscordBot.Bot.Commands.SlashCommands;

public class Help: DBSlashCommand
{
    public string Name => "help";
    public string Description => "This command allows you to check all loaded commands";
    public bool canUseDM => true;

    public bool HasInteraction => false;

    public List<SlashCommandOptionBuilder> Options =>
        new()
        {
            new SlashCommandOptionBuilder()
                .WithName("command")
                .WithDescription("The command you want to get help for")
                .WithRequired(false)
                .WithType(ApplicationCommandOptionType.String)
        };

    public async void ExecuteServer(SocketSlashCommand context)
    {
        EmbedBuilder embedBuilder = new();

        embedBuilder.WithTitle("Help Command");
        embedBuilder.WithColor(Functions.RandomColor);
        var slashCommands = PluginLoader.SlashCommands;
        var options = context.Data.Options;

        //Console.WriteLine("Options: " + options.Count);
        if (options is null || options.Count == 0)
            foreach (var slashCommand in slashCommands)
                embedBuilder.AddField(slashCommand.Name, slashCommand.Description);

        if (options.Count > 0)
        {
            var commandName = options.First().Value;
            var slashCommand = slashCommands.FirstOrDefault(x => x.Name.TrimEnd() == commandName.ToString());
            if (slashCommand is null)
            {
                await context.RespondAsync("Unknown Command " + commandName);
                return;
            }

            embedBuilder.AddField("DM Usable:", slashCommand.canUseDM, true)
                        .WithDescription(slashCommand.Description);
        }

        await context.RespondAsync(embed: embedBuilder.Build());
    }
}
