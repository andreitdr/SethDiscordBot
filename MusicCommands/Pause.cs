using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

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

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            Data.CurrentlyRunning.Paused = true;
        }
    }
}
