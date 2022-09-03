using Discord;
using Discord.Commands;
using Discord.WebSocket;

using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace CMD_LevelingSystem;

internal class Level : DBCommand
{
    public string Command => "level";

    public List<string> Aliases => new() { "lvl" };

    public string Description => "Display tour current level";

    public string Usage => "level";

    public bool requireAdmin => false;

    public async void ExecuteServer(SocketCommandContext context)
    {
        if (!File.Exists(Config.GetValue<string>("LevelingSystemPath") + $"/{context.Message.Author.Id}.dat"))
        {
            await context.Channel.SendMessageAsync("You are now unranked !");
            return;
        }

        var user = await Functions.ConvertFromJson<User>(Config.GetValue<string>("LevelingSystemPath") + $"/{context.Message.Author.Id}.dat");
        if (user == null)
        {
            await context.Channel.SendMessageAsync("You are now unranked !");
            return;
        }

        var builder = new EmbedBuilder();
        var r = new Random();
        builder.WithColor(r.Next(256), r.Next(256), r.Next(256));
        builder.AddField("Current Level", user.CurrentLevel, true)
               .AddField("Current EXP", user.CurrentEXP, true)
               .AddField("Required Exp", user.RequiredEXPToLevelUp, true);
        builder.WithTimestamp(DateTimeOffset.Now);
        await context.Channel.SendMessageAsync(embed: builder.Build());
    }
}
