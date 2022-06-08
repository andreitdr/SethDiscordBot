using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;

namespace CMD_Utils.Music;

internal class Leave : DBCommand
{
    public string Command => "leave";

    public string Description => "Leave the voice channel";

    public string Usage => "leave";

    public bool canUseDM => false;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        if (Data.audioClient is not null && Data.voiceChannel is not null)
        {
            Data.CurrentlyRunning.Stop();
            Data.CurrentlyRunning = null;
            await Data.audioClient.StopAsync();
            await Data.voiceChannel.DisconnectAsync();
        }
    }
}
