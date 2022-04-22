namespace PluginManager.Interfaces
{
    public interface DBCommand
    {
        /// <summary>
        /// Command to be executed
        /// It's CaSe SeNsItIvE
        /// </summary>
        string Command { get; }

        /// <summary>
        /// Command description
        /// </summary>
        string Description { get; }

        /// <summary>
        /// The usage for your command.
        /// It will be displayed when users type help
        /// </summary>
        string Usage { get; }

        /// <summary>
        /// true if the command can be used in a DM channel, otherwise false
        /// </summary>
        bool canUseDM { get; }

        /// <summary>
        /// true if the command can be used in a server, otherwise false
        /// </summary>
        bool canUseServer { get; }

        /// <summary>
        /// true if the command requre admin, otherwise false
        /// </summary>
        bool requireAdmin { get; }

        /// <summary>
        /// The main body of the command. This is what is executed when user calls the command
        /// </summary>
        /// <param name="context">The disocrd Context</param>
        /// <param name="message">The message that the user types</param>
        /// <param name="client">The discord client of the bot</param>
        /// <param name="isDM">true if the message was sent from DM, otherwise false. It is always false if canUseDM is false</param>
        void Execute(Discord.Commands.SocketCommandContext context,
                     Discord.WebSocket.SocketMessage message,
                     Discord.WebSocket.DiscordSocketClient client,
                     bool isDM);
    }
}