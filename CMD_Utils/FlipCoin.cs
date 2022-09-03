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

    public bool requireAdmin => false;

    public async void ExecuteDM(SocketCommandContext context) => ExecuteServer(context);
    public async void ExecuteServer(SocketCommandContext context)
    {
        var random = new System.Random();
        var r = random.Next(1, 3);
        if (r == 1)
            await context.Message.Channel.SendMessageAsync("Heads");
        else
            await context.Message.Channel.SendMessageAsync("Tails");
    }
}
