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
    public class Skip : DBCommand
    {
        public string Command => "skip";

        public string Description => "skip the music that is currently running";

        public string Usage => "skip";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            Data.MusicPlayer.isPlaying = false;
        }
    }
}
