namespace PluginManager.Interfaces
{
    public interface DBCommand
    {
        string Command { get; }

        string Description { get; }

        string Usage { get; }

        bool canUseDM { get; }
        bool canUseServer { get; }

        void Execute(Discord.Commands.SocketCommandContext context,
                     Discord.WebSocket.SocketMessage message,
                     Discord.WebSocket.DiscordSocketClient client,
                     bool isDM);
    }
}