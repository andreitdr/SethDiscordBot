using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;
using System.Collections.Generic;

namespace CMD_Utils;

internal class FlipCoin : DBCommand
{
    public string Command => "flip";

    public List<string> Aliases => null;

    public string Description => "Flip a coin";

    public string Usage => "flip";

    public bool canUseDM => true;

    public bool canUseServer => true;

    public bool requireAdmin => false;

    public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
    {
        var random = new System.Random();
        var r      = random.Next(1, 3);
        if (r == 1)
            await message.Channel.SendMessageAsync("Heads");
        else
            await message.Channel.SendMessageAsync("Tails");
    }
}
