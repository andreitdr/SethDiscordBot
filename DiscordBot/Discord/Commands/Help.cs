using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Loaders;
using PluginManager.Interfaces;
using PluginManager.Others.Permissions;

namespace PluginManager.Commands
{
    internal class Help : DBCommand
    {
        public string Command => "help";

        public string Description => "This command allows you to check all loadded commands";

        public string Usage => "help";

        public bool canUseDM => true;
        public bool canUseServer => true;

        public bool requireAdmin => false;

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            bool isAdmin = ((SocketGuildUser)message.Author).isAdmin();
            if (isAdmin)
            {
                if (isDM)
                {
                    foreach (DBCommand p in PluginLoader.Plugins!)
                        if (p.canUseDM)
                            if (p.requireAdmin)
                                context.Channel.SendMessageAsync("[ADMIN] " + p.Usage + "\t" + p.Description);
                            else context.Channel.SendMessageAsync(p.Usage + "\t" + p.Description);
                }
                else
                {
                    foreach (DBCommand p in PluginLoader.Plugins!)
                        if (p.canUseServer)
                            if (p.requireAdmin)
                                context.Channel.SendMessageAsync("[ADMIN] " + p.Usage + "\t" + p.Description);
                            else context.Channel.SendMessageAsync(p.Usage + "\t" + p.Description);
                }
            }
            else
            {
                if (isDM)
                {
                    foreach (DBCommand p in PluginLoader.Plugins!)
                        if (p.canUseDM && !p.requireAdmin)
                            context.Channel.SendMessageAsync(p.Usage + "\t" + p.Description);
                }
                else
                {
                    foreach (DBCommand p in PluginLoader.Plugins!)
                        if (p.canUseServer && !p.requireAdmin)
                            context.Channel.SendMessageAsync(p.Usage + "\t" + p.Description);
                }
            }


        }
    }
}