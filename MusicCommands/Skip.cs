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

        public List<string> Aliases => null;

        public string Description => "skip the music that is currently running";

        public string Usage => "skip";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            var loadedSong = Data.MusicPlayer.NowPlaying;

            if (loadedSong is null || Data.MusicPlayer.isPlaying == false)
            {
                message.Channel.SendMessageAsync("There is no music playing");
                return;
            }

            Data.MusicPlayer.isPlaying = false;
            message.Channel.SendMessageAsync($"You have skipped {loadedSong.Name}");
        }
    }
}
