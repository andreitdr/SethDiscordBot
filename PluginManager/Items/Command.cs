using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using PluginManager.Others;

namespace PluginManager.Items;

public class Command
{
    /// <summary>
    ///     The author of the command
    /// </summary>
    public SocketUser? Author;

    /// <summary>
    ///     The Command class contructor
    /// </summary>
    /// <param name="message">The message that was sent</param>
    public Command(SocketMessage message)
    {
        Author = message.Author;
        var data = message.Content.Split(' ');
        Arguments   = data.Length > 1 ? new List<string>(data.MergeStrings(1).Split(' ')) : new List<string>();
        CommandName = data[0].Substring(1);
        PrefixUsed  = data[0][0];
    }

    /// <summary>
    ///     The list of arguments
    /// </summary>
    public List<string> Arguments { get; }

    /// <summary>
    ///     The command that is executed
    /// </summary>
    public string CommandName { get; }

    /// <summary>
    ///     The prefix that is used for the command
    /// </summary>
    public char PrefixUsed { get; }
}

public class ConsoleCommand
{
    public string           CommandName { get; init; }
    public string           Description { get; init; }
    public string           Usage       { get; init; }
    public Action<string[]> Action      { get; init; }
}