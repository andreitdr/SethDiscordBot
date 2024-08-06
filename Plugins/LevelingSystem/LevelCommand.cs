using Discord;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace LevelingSystem;

internal class LevelCommand: IDbCommand
{
    public string Command => "level";

    public List<string> Aliases => ["lvl", "rank"];

    public string Description => "Display tour current level";

    public string Usage => "level";

    public bool RequireAdmin => false;

    public async void ExecuteServer(DbCommandExecutingArguments args)
    {
        if(Variables.Database is null)
        {
            Application.Logger.Log("Database is not initialized", this, LogType.Warning);
            return;
        }


        object[]? user = await Variables.Database.ReadDataArrayAsync($"SELECT * FROM Levels WHERE UserID=@userId",
                               new KeyValuePair<string, object>("userId", args.Context.Message.Author.Id));


        if (user is null)
        {
            await args.Context.Channel.SendMessageAsync("You are now unranked !");
            return;
        }

        var level = (int)user[1];
        var exp   = (int)user[2];

        var builder = new EmbedBuilder();
        var r       = new Random();
        builder.WithColor(r.Next(256), r.Next(256), r.Next(256));
        builder.AddField("Current Level", level, true)
               .AddField("Current EXP", exp, true)
               .AddField("Required Exp", (level * 8 + 24).ToString(), true);
        builder.WithTimestamp(DateTimeOffset.Now);
        builder.WithAuthor(args.Context.Message.Author);
        await args.Context.Channel.SendMessageAsync(embed: builder.Build());
    }
}
