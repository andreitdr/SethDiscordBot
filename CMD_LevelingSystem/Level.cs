using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace CMD_LevelingSystem
{
    internal class Level : DBCommand
    {
        public string Command => "level";

        public string Description => "Display tour current level";

        public string Usage => "level";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            User user = await Functions.ConvertFromJson<User>(Config.GetValue("LevelingSystemPath") + $"/{message.Author.Id}.dat");
            if (user == null)
            {
                await context.Channel.SendMessageAsync("You are now unranked !");
                return;
            }

            var    builder = new EmbedBuilder();
            Random r       = new Random();
            builder.WithColor(r.Next(256), r.Next(256), r.Next(256));
            builder.AddField("Current Level", user.CurrentLevel, true)
                   .AddField("Current EXP", user.CurrentEXP, true)
                   .AddField("Required Exp", user.RequiredEXPToLevelUp, true);
            builder.WithTimestamp(DateTimeOffset.Now);
            await context.Channel.SendMessageAsync(embed: builder.Build());
        }
    }
}
