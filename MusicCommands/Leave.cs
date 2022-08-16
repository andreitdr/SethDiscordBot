using System.Collections.Generic;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;

namespace MusicCommands;

internal class Leave : DBCommand
{
    public string Command => "leave";

    public List<string> Aliases => null;

    public string Description => "Leave the voice channel";

    public string Usage => "leave";

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        if (Data.audioClient is not null && Data.voiceChannel is not null)
        {
            await Data.audioClient.StopAsync();
            await Data.voiceChannel.DisconnectAsync();
        }

        if (Data.Playlist is not null)
        {
            Data.Playlist.ClearQueue();
            Data.Playlist = new();
        }

        if (Data.MusicPlayer is not null)
        {
            Data.MusicPlayer.Stop();
            Data.MusicPlayer = null;
        }
    }
}
