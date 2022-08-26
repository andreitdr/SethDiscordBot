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

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        Data.MusicPlayer.isPaused = !Data.MusicPlayer.isPaused;
    }
}
