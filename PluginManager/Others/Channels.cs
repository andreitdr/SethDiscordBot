using Discord;

using System.Threading.Tasks;

namespace PluginManager.Others
{
    public static class ChannelManagement
    {
        public static IGuildChannel GetTextChannel(this IGuild server, string name) => server.GetTextChannel(name);
        public static IGuildChannel GetVoiceChannel(this IGuild server, string name) => server.GetVoiceChannel(name);
        public static async Task<IDMChannel> GetDMChannel(IGuildUser user) => await user.CreateDMChannelAsync();
        public static IChannel GetChannel(IMessage message) => message.Channel;

    }
}