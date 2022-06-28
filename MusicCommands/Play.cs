using System;
using System.Diagnostics;
using System.IO;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace MusicCommands;

internal class Play : DBCommand
{
    public string Command => "fplay";

    public string Description => "Play music from a file";

    public string Usage => "fplay [name]";

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        var path     = "./Music";
        var FileName = Functions.GetArguments(message).ToArray().MergeStrings(0);
        path += "/" + FileName + ".ogg";
        if (!File.Exists(path))
        {
            Console.WriteLine("Unknown path " + path);
            return;
        }


        Data.voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
        if (Data.voiceChannel == null)
        {
            await context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument.");
            return;
        }

        Data.audioClient = await Data.voiceChannel.ConnectAsync();

        using (var ffmpeg = CreateStream(path))
        using (var output = ffmpeg.StandardOutput.BaseStream)
        using (var discord = Data.audioClient.CreatePCMStream(AudioApplication.Mixed))
        {
            if (Data.CurrentlyRunning != null) Data.CurrentlyRunning.Stop();
            Data.CurrentlyRunning = new MusicPlayer(output, discord);
            await Data.CurrentlyRunning.StartSendAudio();
        }
    }

    private Process CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo { FileName = "ffmpeg", Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1", UseShellExecute = false, RedirectStandardOutput = true });
    }
}
