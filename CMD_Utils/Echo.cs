using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;
using System.Collections.Generic;

internal class Echo : DBCommand
{
    public string Command => "echo";

    public List<string> Aliases => null;

    public string Description => "Replay with the same message";

    public string Usage => "echo [message]";

    public bool canUseDM     => true;
    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        var m = message.Content.Substring(6);
        await message.Channel.SendMessageAsync(m);
    }
}
