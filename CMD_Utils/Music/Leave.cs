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
    class Leave : DBCommand
    {
        public string Command => "leave";

        public string Description => "Leave the voice channel";

        public string Usage => "leave";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            await Data.audioClient.StopAsync();
            await Data.voiceChannel.DisconnectAsync();
        }
    }
}
