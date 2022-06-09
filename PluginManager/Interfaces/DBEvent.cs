using Discord.WebSocket;

namespace PluginManager.Interfaces;

public interface DBEvent
{
    /// <summary>
    ///     The name of the event
    /// </summary>
    string name { get; }

    /// <summary>
    ///     The description of the event
    /// </summary>
    string description { get; }

    /// <summary>
    ///     The method that is invoked when the event is loaded into memory
    /// </summary>
    /// <param name="client">The discord bot client</param>
    void Start(DiscordSocketClient client);
}
