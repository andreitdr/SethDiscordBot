using Discord.WebSocket;

namespace DiscordBotCore.Interfaces;

public interface DBEvent
{
    /// <summary>
    ///     The name of the event
    /// </summary>
    string Name { get; }

    /// <summary>
    ///     The description of the event
    /// </summary>
    string Description { get; }

    /// <summary>
    ///    If the event requires another thread to run
    /// </summary>
    bool RequireOtherThread { get; }

    /// <summary>
    ///     The method that is invoked when the event is loaded into memory
    /// </summary>
    /// <param name="client">The discord bot client</param>
    void Start(DiscordSocketClient client);
}
