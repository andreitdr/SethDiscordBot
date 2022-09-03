using System;
using System.Collections.Generic;
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
    public string Command => "play";

    public List<string> Aliases => new() { "p" };

    public string Description => "Play music from a file";

    public string Usage => "play [name/url]";

    public bool requireAdmin => false;

    public async void ExecuteServer(SocketCommandContext context)
    {
        Directory.CreateDirectory("Music");
        var path = "./Music/";
        string[] splitted = context.Message.Content.Split(' ');
        if (splitted.Length < 2)
            return;
        do
        {
            if (splitted.Length == 2 && splitted[1].Contains("youtube.com") || splitted[1].Contains("youtu.be"))
            {
                var url = splitted[1];
                path += $"{Functions.CreateMD5(url)}";
                if (File.Exists(path))
                {
                    Data.Playlist.Enqueue(new AudioFile(path, null));
                }
                else
                {
                    var file = new AudioFile(path, url);
                    await file.DownloadAudioFile();
                    Data.Playlist.Enqueue(file);
                }
            }
            else
            {
                var searchString = splitted.MergeStrings(1);
                path += $"{Functions.CreateMD5(searchString)}";
                if (File.Exists(path))
                {
                    Data.Playlist.Enqueue(new AudioFile(path, null));
                }
                else
                {
                    await context.Channel.SendMessageAsync("Searching for " + searchString);
                    var file = new AudioFile(path, searchString);
                    await file.DownloadAudioFile();
                    Data.Playlist.Enqueue(file);
                    if (Data.MusicPlayer is null)
                        await context.Channel.SendMessageAsync("Playing: " + searchString);
                }
            }

            if (Data.MusicPlayer is not null)
            {
                await context.Channel.SendMessageAsync("Queued your request: " + splitted.MergeStrings(1));
                return;
            }
        }
        while (false); // run only one time !


        Data.voiceChannel = (context.User as IGuildUser)?.VoiceChannel;

        if (Data.voiceChannel == null)
        {
            await context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument.");
            return;
        }

        if (Data.audioClient is null)
        {
            Data.audioClient = await Data.voiceChannel.ConnectAsync(true);
            Data.MusicPlayer = null;
        }


        using (var discordChanneAudioOutStream = Data.audioClient.CreatePCMStream(AudioApplication.Mixed))
        {
            Data.MusicPlayer ??= new MusicPlayer(discordChanneAudioOutStream);
            while (Data.Playlist.Count > 0)
            {
                var nowPlaying = Data.Playlist.GetNextSong;
                using (var ffmpeg = CreateStream(nowPlaying.Name))
                using (var ffmpegOutputBaseStream = ffmpeg.StandardOutput.BaseStream)
                {
                    await Data.MusicPlayer.Play(ffmpegOutputBaseStream, 1024, nowPlaying);
                    Console.WriteLine("Finished playing from " + nowPlaying.Url);
                }
            }

            Data.MusicPlayer = null;
        }
    }

    private static Process CreateStream(string path)
    {
        return Process.Start(new ProcessStartInfo { FileName = "ffmpeg", Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1", UseShellExecute = false, RedirectStandardOutput = true });
    }
}
