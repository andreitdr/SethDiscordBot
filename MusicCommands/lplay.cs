using CMD_Utils.Music;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;

namespace MusicCommands;

internal class lplay : DBCommand
{
    public string Command => "lplay";

    public string Description => "Play music from a link";

    public string Usage => "lplay [url]";

    public bool canUseDM => false;

    public bool canUseServer => false;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        var URL = message.Content.Split(' ')[1];
        if (!URL.EndsWith(".mp3") && !URL.EndsWith(".wav") && !URL.EndsWith(".flac") && !URL.EndsWith(".ogg"))
        {
            await message.Channel.SendMessageAsync("Invalid URL");
            return;
        }

        Data.voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
        if (Data.voiceChannel == null)
        {
            await context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument.");
            return;
        }

        Data.audioClient = await Data.voiceChannel.ConnectAsync();

        using (var discord = Data.audioClient.CreatePCMStream(AudioApplication.Mixed))
        {
            await message.Channel.SendMessageAsync("Loading...");

            Data.CurrentlyRunning = new MusicPlayer(discord);
            await Data.CurrentlyRunning.StartSendAudioFromLink(URL);
        }
    }
}
