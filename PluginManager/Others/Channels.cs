using Discord;

using System.Threading.Tasks;

namespace PluginManager.Others
{
    /// <summary>
    ///    A class that handles the sending of messages to the user.
    /// </summary>
    public static class ChannelManagement
    {
        /// <summary>
        /// Get the text channel by name from server
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="name">The channel name</param>
        /// <returns><see cref="IGuildChannel"/></returns>
        public static IGuildChannel GetTextChannel(this IGuild server, string name) => server.GetTextChannel(name);
        /// <summary>
        /// Get the voice channel by name from server
        /// </summary>
        /// <param name="server">The server</param>
        /// <param name="name">The channel name</param>
        /// <returns><see cref="IGuildChannel"/></returns>
        public static IGuildChannel GetVoiceChannel(this IGuild server, string name) => server.GetVoiceChannel(name);

        /// <summary>
        /// Get the DM channel between <see cref="Discord.WebSocket.DiscordSocketClient"/> and <see cref="IGuildUser"/>
        /// </summary>
        /// <param name="user"></param>
        /// <returns><see cref="IDMChannel"/></returns>
        public static async Task<IDMChannel> GetDMChannel(IGuildUser user) => await user.CreateDMChannelAsync();

        /// <summary>
        /// Get the channel where the message was sent
        /// </summary>
        /// <param name="message">The message</param>
        /// <returns><see cref="IChannel"/></returns>
        public static IChannel GetChannel(IMessage message) => message.Channel;

    }
}