using Discord;
using Discord.Audio;

namespace MusicCommands;

internal static class Data
{
    internal static IAudioClient  audioClient  = null;
    internal static IVoiceChannel voiceChannel = null;

    internal static MusicPlayer   MusicPlayer = null;
    internal static MusicPlaylist Playlist    = new();
}
