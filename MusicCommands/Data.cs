using Discord;
using Discord.Audio;
using MusicCommands;

namespace CMD_Utils.Music;

internal static class Data
{
    internal static IAudioClient  audioClient  = null;
    internal static IVoiceChannel voiceChannel = null;

    internal static MusicPlayer CurrentlyRunning = null;
}
