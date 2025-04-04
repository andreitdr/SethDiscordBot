using Discord.Audio;

namespace MusicPlayer;

public class Variables
{
    public static Dictionary<string, MusicInfo> _MusicDatabase;
    public static MusicPlayer  _MusicPlayer;

    public static IAudioClient audioClient;
}
