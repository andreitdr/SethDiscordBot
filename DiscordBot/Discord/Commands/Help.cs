using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Loaders;
using PluginManager.Interfaces;

namespace PluginManager.Commands
{
    internal class Help : DBCommand
    {
        public string Command => "help";

        public string Description => "This command allows you to check all loadded commands";

        public string Usage => "help";

        public bool canUseDM => true;
        public bool canUseServer => true;

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            if (isDM)
            {
                foreach (DBCommand p in PluginLoader.Plugins!)
                    if (p.canUseDM)
                        context.Channel.SendMessageAsync(p.Usage + "\t" + p.Description);
            }
            else
            {
                foreach (DBCommand p in PluginLoader.Plugins!)
                    if (p.canUseServer)
                        context.Channel.SendMessageAsync(p.Usage + "\t" + p.Description);
            }

        }
    }
}