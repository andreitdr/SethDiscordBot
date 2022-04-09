using CMD_Utils.Music;

using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCommands
{
    class Unpause : DBCommand
    {
        public string Command => "unpause";

        public string Description => "Unpause the music";

        public string Usage => "unpause";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            Data.CurrentlyRunning.Paused = false;
        }
    }
}
