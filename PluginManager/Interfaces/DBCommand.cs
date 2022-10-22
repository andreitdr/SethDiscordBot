using System.Collections.Generic;

using Discord.Commands;

namespace PluginManager.Interfaces;

public interface DBCommand
{
    /// <summary>
    ///     Command to be executed
    ///     It's CaSe SeNsItIvE
    /// </summary>
    string Command { get; }

    /// <summary>
    ///     Command aliases. Users may use this to execute the command
    /// </summary>
    List<string>? Aliases { get; }

    /// <summary>
    ///     Command description
    /// </summary>
    string Description { get; }

    /// <summary>
    ///     The usage for your command.
    ///     It will be displayed when users type help
    /// </summary>
    string Usage { get; }

    /// <summary>
    ///     true if the command requre admin, otherwise false
    /// </summary>
    bool requireAdmin { get; }

    /// <summary>
    ///     The main body of the command. This is what is executed when user calls the command in Server
    /// </summary>
    /// <param name="context">The disocrd Context</param>
    void ExecuteServer(SocketCommandContext context)
    {
    }

    /// <summary>
    ///     The main body of the command. This is what is executed when user calls the command in DM
    /// </summary>
    /// <param name="context">The disocrd Context</param>
    void ExecuteDM(SocketCommandContext context)
    {
    }
}