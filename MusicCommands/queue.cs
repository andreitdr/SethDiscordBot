using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;

namespace MusicCommands
{
    public class queue : DBCommand
    {
        public string Command => "queue";

        public string Description => "check queue";

        public string Usage => "queue";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            await context.Channel.SendMessageAsync($"You have {Data.Playlist.Count} items in queue");
        }
    }
}
