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

        public bool requireAdmin => false;

        public async void ExecuteServer(SocketCommandContext context)
        {
            var loadedSong = Data.MusicPlayer.NowPlaying;

            if (loadedSong is null || Data.MusicPlayer.isPlaying == false)
            {
                await context.Message.Channel.SendMessageAsync("There is no music playing");
                return;
            }

            Data.MusicPlayer.isPlaying = false;
            await context.Message.Channel.SendMessageAsync($"You have skipped {loadedSong.Name}");
        }
    }
}
