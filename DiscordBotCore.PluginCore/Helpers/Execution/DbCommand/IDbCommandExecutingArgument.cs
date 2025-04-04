using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore.Logging;

namespace DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;

public interface IDbCommandExecutingArgument
{
    ILogger Logger { get; init; }
    string CleanContent { get; init; }
    string CommandUsed { get; init; }
    string[]? Arguments { get; init; }

    SocketCommandContext Context { get; init; }
    public DirectoryInfo PluginBaseDirectory { get; init; }
}