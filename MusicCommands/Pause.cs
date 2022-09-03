using System.Collections.Generic;

using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

namespace MusicCommands;

internal class Pause : DBCommand
{
    public string Command => "pause";

    public List<string> Aliases => null;

    public string Description => "Pause/Unpause the music that is currently running";

    public string Usage => "pause";

    public bool requireAdmin => false;

    public void ExecuteServer(SocketCommandContext context)
    {
        Data.MusicPlayer.isPaused = !Data.MusicPlayer.isPaused;
    }
}
