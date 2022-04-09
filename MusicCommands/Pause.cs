using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CMD_Utils.Music
{
    class Pause : DBCommand
    {
        public string Command => "pause";

        public string Description => "Pause the music";

        public string Usage => "pause";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            // to be implemented
        }
    }
}
