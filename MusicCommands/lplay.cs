using CMD_Utils.Music;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using Discord;

using PluginManager.Interfaces;
using PluginManager.Others;

namespace MusicCommands
{
    class lplay : DBCommand
    {
        public string Command => "lplay";

        public string Description => "Play music from a link";

        public string Usage => "lplay [name]";

        public bool canUseDM => false;

        public bool canUseServer => false;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {

            Data.voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
            if (Data.voiceChannel == null) { await context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            Data.audioClient = await Data.voiceChannel.ConnectAsync();

            using (var discord = Data.audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                if (Data.CurrentlyRunning != null)
                    Data.CurrentlyRunning.Stop();
                LinkMusic music = new LinkMusic(Functions.GetArguments(message)[0]);
                Data.CurrentlyRunning = new MusicPlayer(await music.GetStream(), discord);
                await Data.CurrentlyRunning.StartSendAudio();
            }
        }

    }
}
