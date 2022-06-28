using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;

namespace MusicCommands;

internal class Unpause : DBCommand
{
    public string Command => "unpause";

    public string Description => "Unpause the music";

    public string Usage => "unpause";

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        Data.CurrentlyRunning.Paused = false;
    }
}
