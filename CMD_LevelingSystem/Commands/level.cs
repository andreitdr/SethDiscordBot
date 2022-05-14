using Discord;
using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;
using PluginManager.LanguageSystem;

using System;


public class level : DBCommand
{
    public string Command => "rank";

    public string Description => "Display your current level";

    public string Usage => "rank";

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {

        try
        {
            int cLv = Data.GetLevel(message.Author.Id);
            Int64 cEXP = Data.GetExp(message.Author.Id);
            Int64 rEXP = Data.GetReqEXP(message.Author.Id);

            var embed = new EmbedBuilder()
            {
                Title = "Leveling System",
                Description = message.Author.Mention
            };
            Random r = new Random();
            int _r = r.Next(0, 256);
            int _g = r.Next(0, 256);
            int _b = r.Next(0, 256);
            embed.WithColor(new Color(_r, _g, _b));
            embed.AddField("Level", cLv);
            embed.AddField("Current EXP", cEXP);
            embed.AddField("Required Exp to Level up", rEXP);
            embed.WithCurrentTimestamp();
            await message.Channel.SendMessageAsync(embed: embed.Build());
        }
        catch
        {
            if (Language.ActiveLanguage != null)
                await message.Channel.SendMessageAsync(Language.ActiveLanguage.LanguageWords["DB_COMMAND_RANK_NO_RANK"]);
            else await message.Channel.SendMessageAsync("You are unranked now. Please type a message in chat that is not a command and try again this command");
            return;
        }


    }
}

