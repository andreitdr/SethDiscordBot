using Discord;
using Discord.Audio;

namespace YoutubeExtension.Downloader;

internal static class Data
{
    internal static IAudioClient  audioClient  = null;
    internal static IVoiceChannel voiceChannel = null;

    internal static MusicPlayer CurrentlyRunning = null;
}
