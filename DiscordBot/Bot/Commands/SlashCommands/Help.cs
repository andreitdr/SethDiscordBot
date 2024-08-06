using System;
using System.Collections.Generic;
using System.Linq;
using Discord;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Loaders;
using DiscordBotCore.Others;

namespace DiscordBot.Bot.Commands.SlashCommands;

public class Help: IDbSlashCommand
{
    public string Name => "help";
    public string Description => "This command allows you to check all loaded commands";
    public bool CanUseDm => true;

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

        var random = new Random();
        Color c = new Color(random.Next(0, 255), random.Next(0, 255), random.Next(0, 255));

        embedBuilder.WithColor(c);
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

            embedBuilder.AddField("DM Usable:", slashCommand.CanUseDm, true)
                        .WithDescription(slashCommand.Description);
        }

        await context.RespondAsync(embed: embedBuilder.Build());
    }
}
