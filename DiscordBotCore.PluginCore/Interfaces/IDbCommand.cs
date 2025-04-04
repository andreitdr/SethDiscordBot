using DiscordBotCore.Logging;
using DiscordBotCore.PluginCore.Helpers;
using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;

namespace DiscordBotCore.PluginCore.Interfaces;

public interface IDbCommand
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
    bool RequireAdmin { get; }

    /// <summary>
    ///     The main body of the command. This is what is executed when user calls the command in Server
    /// </summary>
    /// <param name="args">The Discord Context</param>
    Task ExecuteServer(IDbCommandExecutingArgument args) => Task.CompletedTask;

    /// <summary>
    ///     The main body of the command. This is what is executed when user calls the command in DM
    /// </summary>
    /// <param name="args">The Discord Context</param>
    Task ExecuteDm(IDbCommandExecutingArgument args) => Task.CompletedTask;
}
