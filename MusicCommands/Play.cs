using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Others;

namespace MusicCommands;

internal class Play : DBCommand
{
    public string Command => "play";

    public string Description => "Play music from a file";

    public string Usage => "fplay [name/url]";

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        Directory.CreateDirectory("Music");
        var      path     = "./Music/";
        string[] splitted = message.Content.Split(' ');
        if (splitted.Length < 2)
            return;
        do
        {
            if (splitted.Length == 2)
            {
                if (!splitted[1].Contains("youtube.com"))
                {
                    await context.Channel.SendMessageAsync("Invalid link");
                    return;
                }

                string url = splitted[1];
                path += $"{Functions.CreateMD5(url)}";
                if (File.Exists(path))
                    break;
                //await context.Channel.SendMessageAsync("Searching for " + url);
                await GetMusicAudio(url, path);
                //await context.Channel.SendMessageAsync("Playing: " + url);
            }
            else
            {
                string searchString = Functions.MergeStrings(splitted, 1);
                path += $"{Functions.CreateMD5(searchString)}";

                if (File.Exists(path))
                    break;
                await context.Channel.SendMessageAsync("Searching for " + searchString);
                await GetMusicAudio(searchString, path);
                await context.Channel.SendMessageAsync("Playing: " + searchString);
            }
        }
        while (false);


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

    private async Task GetMusicAudio(string url, string location)
    {
        Process proc = new Process();
        proc.StartInfo.FileName               = "MusicDownloader.exe";
        proc.StartInfo.Arguments              = $"{url},{location}";
        proc.StartInfo.UseShellExecute        = false;
        proc.StartInfo.RedirectStandardOutput = true;

        proc.Start();
        await proc.WaitForExitAsync();
    }
}
