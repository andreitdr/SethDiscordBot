using DiscordBotCore.PluginCore.Helpers;
using DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;

namespace DiscordBotCore.PluginCore.Interfaces;

public interface IDbEvent
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
    ///     The method that is invoked when the event is loaded into memory
    /// </summary>
    /// <param name="args">The arguments for the start method</param>
    void Start(IDbEventExecutingArgument args);
}
