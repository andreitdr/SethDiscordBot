using Discord;
using Discord.WebSocket;

using PluginManager;
using PluginManager.Interfaces;

using SlashCommands.Items;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD_Utils
{
    public class SlashRandom : DBSlashCommand
    {
        public string Command => "random";

        public string Description => "Random number";

        public string Usage => "random [min] [max]";

        public bool requireAdmin => false;

        public bool PrivateResponse => true;

        public async Task InitializeCommand(DiscordSocketClient client)
        {
            var guildCommand = new SlashCommandBuilder();
            guildCommand.WithName(Command);
            guildCommand.WithDescription(Description);
            guildCommand.AddOption(new SlashCommandOptionBuilder()
                .WithName("min")
                .WithDescription("Minimum number")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer));
            guildCommand.AddOption(new SlashCommandOptionBuilder()
                .WithName("max")
                .WithDescription("Maximum number")
                .WithRequired(true)
                .WithType(ApplicationCommandOptionType.Integer));
            await client.GetGuild(ulong.Parse(Config.GetValue<string>("ServerID"))).CreateApplicationCommandAsync(guildCommand.Build());
        }

        public async Task ExecuteServer(SocketSlashCommand command)
        {
            var commandArguments = command.Data.Options.ToArray();

            if (commandArguments.Count() == 0)
            {
                await command.RespondAsync("Please provide a min and max value", ephemeral: true);
                return;
            }

            var min = (int)commandArguments[0].Value;
            var max = (int)commandArguments[1].Value;


            await command.RespondAsync("User generated number: " + new System.Random().Next(min, max + 1), ephemeral: PrivateResponse);

        }
    }
}
