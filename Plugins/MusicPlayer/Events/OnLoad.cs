using Discord.WebSocket;

using DiscordBotCore;
using DiscordBotCore.Interfaces;

namespace MusicPlayer.Events;

public class OnLoad: IDbEvent
{
    private static readonly string _DefaultMusicPath = "Music/";
    private static readonly string _DefaultSaveLocation = "Music/Melodies/";
    private static readonly string _DefaultMusicDB   = "Music/music_db.json";
    public string Name => "Music Commands";
    public string Description => "The default music commands event loader";

    public async void Start(DiscordSocketClient client)
    {
        var path1    = Application.GetResourceFullPath(_DefaultMusicPath);
        var path2    = Application.GetResourceFullPath(_DefaultSaveLocation);
        var path3    = Application.GetResourceFullPath(_DefaultMusicDB);
        Directory.CreateDirectory(path1);
        Directory.CreateDirectory(path2);
        Variables._MusicDatabase = new MusicDatabase(path3);
        await Variables._MusicDatabase.LoadFromFile();
    }
}
