using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

namespace CMD_Utils
{
    class FlipCoin : DBCommand
    {
        public string Command => "flip";

        public string Description => "Flip a coin";

        public string Usage => "flip";

        public bool canUseDM => true;

        public bool canUseServer => true;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            System.Random random = new System.Random();
            int r = random.Next(1, 3);
            if (r == 1)
                await message.Channel.SendMessageAsync("Heads");
            else await message.Channel.SendMessageAsync("Tails");
        }
    }
}
