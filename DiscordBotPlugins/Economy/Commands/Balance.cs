using Discord;
using PluginManager.Interfaces;
using Discord.WebSocket;
namespace Economy.Commands;

public class Balance : DBSlashCommand
{
    public string Name => "balance";

    public string Description => "Change or view info about your balance";

    public bool canUseDM => false; // server only

    public List<SlashCommandOptionBuilder> Options => new List<SlashCommandOptionBuilder>()
    {
        new SlashCommandOptionBuilder(){Name="send", Description="Send money to another user", Type=ApplicationCommandOptionType.SubCommand, Options=new List<SlashCommandOptionBuilder>()
        {
            new SlashCommandOptionBuilder(){Name="user", Description="The user to send money to", Type=ApplicationCommandOptionType.User, IsRequired=true},
            new SlashCommandOptionBuilder(){Name="amount", Description="The amount of money to send", Type=ApplicationCommandOptionType.Number, IsRequired=true}
        }},

        new SlashCommandOptionBuilder(){Name="info", Description="View info about your balance", Type=ApplicationCommandOptionType.SubCommand}
    };

    public async void ExecuteServer(SocketSlashCommand context)
    {
        var option = context.Data.Options.FirstOrDefault();
        var guild = context.User.MutualGuilds.FirstOrDefault(g => g.Id == context.GuildId);
        if (option.Name == "send")
        {
            var options = option.Options.ToArray();
            Console.WriteLine(options.Length);
            var user = options[0].Value as IUser;
            var amount = options[1].Value as float?;

            if (amount == null)
            {
                await context.RespondAsync("The amount is invalid");
                return;
            }

            if (user == null)
            {
                await context.RespondAsync("The user is invalid");
                return;
            }

            if (user.Id == context.User.Id)
            {
                await context.RespondAsync("You can't send money to yourself");
                return;
            }

            var balance = await Engine.GetBalance(context.User.Id);
            if (balance < amount)
            {
                await context.RespondAsync("You don't have enough money to send");
                return;
            }

            await Engine.RemoveMoney(context.User.Id, amount.Value);
            await Engine.AddMoney(user.Id, amount.Value);

            await context.RespondAsync($"You sent {amount} to {guild.GetUser(user.Id).Mention}");
        }
        else if (option.Name == "info")
        {
            var balance = await Engine.GetBalance(context.User.Id);
            await context.RespondAsync($"Your balance is {balance}");
        }
    }
}
