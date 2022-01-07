using Discord.WebSocket;

namespace PluginManager.Interfaces
{
    public interface DBEvent
    {
        string name { get; }
        string description { get; }

        void Start(DiscordSocketClient client);
    }
}
