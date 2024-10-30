using System.Diagnostics;
using Discord.Audio;

using DiscordBotCore;
using DiscordBotCore.Others;

namespace MusicPlayer;

public class MusicPlayer
{
    private static int defaultByteSize = 1024;

    public Queue<MusicInfo> MusicQueue { get; private set; }

    public bool isPaused { get; private set; }
    public bool isPlaying { get; private set; }

    private bool isQueueRunning;
    public int ByteSize { get; private set; }

    public MusicInfo? CurrentlyPlaying { get; private set; }

    public MusicPlayer()
    {
        MusicQueue = new Queue<MusicInfo>();
    }

    public async Task PlayQueue()
    {
        if (isQueueRunning)
        {
            Application.CurrentApplication.Logger.Log("Another queue is running !", typeof(MusicPlayer), LogType.Warning);
            return;
        }

        if (Variables.audioClient is null)
        {
            Application.CurrentApplication.Logger.Log("Audio Client is null", typeof(MusicPlayer), LogType.Warning);
            return;
        }

        isQueueRunning = true;

        string? ffmpegPath = await Application.CurrentApplication.PluginManager.GetDependencyLocation("FFMPEG");
        if(ffmpegPath is null)
        {
            Application.CurrentApplication.Logger.Log("FFMPEG is missing. Please install it and try again.", typeof(MusicPlayer), LogType.Error);
            isQueueRunning = false;
            return;
        }
        
        ffmpegPath = ffmpegPath.Replace("\\", "/");
        ffmpegPath = Path.GetFullPath(ffmpegPath);
        while (MusicQueue.TryDequeue(out var dequeuedMusic))
        {
            CurrentlyPlaying = dequeuedMusic;
            await using var dsAudioStream = Variables.audioClient.CreatePCMStream(AudioApplication.Mixed);
            using var       ffmpeg        = CreateStream(ffmpegPath, CurrentlyPlaying.Location);
            if (ffmpeg is null)
            {
                Application.CurrentApplication.Logger.Log($"Failed to start ffmpeg process. FFMPEG is missing or the {CurrentlyPlaying.Location} has an invalid format.", typeof(MusicPlayer), LogType.Error);
                continue;
            }
            await using var ffmpegOut = ffmpeg.StandardOutput.BaseStream;
            await PlayCurrentTrack(dsAudioStream, ffmpegOut, CurrentlyPlaying.ByteSize ?? defaultByteSize);
        }
        isQueueRunning   = false;
        CurrentlyPlaying = null;
    }

    public void Loop(int numberOfTimes)
    {
        if (CurrentlyPlaying is null) return;

        Queue<MusicInfo> tempQueue = new();
        for (var i = 0; i < numberOfTimes; i++)
        {
            tempQueue.Enqueue(CurrentlyPlaying);
        }

        foreach (var musicInfo in MusicQueue)
        {
            tempQueue.Enqueue(musicInfo);
        }

        MusicQueue = tempQueue;
    }

    private async Task PlayCurrentTrack(Stream discordVoiceChannelStream, Stream fileStreamFfmpeg, int byteSize)
    {
        if (isPlaying) return;
        ByteSize = byteSize;

        isPlaying = true;
        isPaused  = false;

        while (isPlaying)
        {
            if (isPaused) continue;

            var bits = new byte[byteSize];
            var read = await fileStreamFfmpeg.ReadAsync(bits, 0, ByteSize);
            if (read == 0) break;

            try
            {
                await discordVoiceChannelStream.WriteAsync(bits, 0, read);
            }
            catch (Exception ex)
            {
                Application.CurrentApplication.Logger.LogException(ex, this);
                break;
            }
        }



        await discordVoiceChannelStream.FlushAsync();
        await fileStreamFfmpeg.FlushAsync();

        isPlaying = false;
        isPaused  = false;
    }

    public void Pause()
    {
        isPaused = true;
    }

    public void Unpause()
    {
        isPaused = false;
    }

    public bool Enqueue(MusicInfo music)
    {
        MusicQueue.Enqueue(music);
        return true;
    }

    public void Skip()
    {
        isPlaying = false;
    }

    private static Process? CreateStream(string? fileName, string path)
    {
        return Process.Start(new ProcessStartInfo
            {
                FileName               = fileName,
                Arguments              = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute        = false,
                RedirectStandardOutput = true
            }
        );
    }

    public void Stop()
    {
        MusicQueue.Clear();
        isPlaying = false;
    }

}
