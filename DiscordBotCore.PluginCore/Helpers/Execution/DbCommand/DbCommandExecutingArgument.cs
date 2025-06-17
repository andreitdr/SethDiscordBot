using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore.Logging;

namespace DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;

public class DbCommandExecutingArgument : IDbCommandExecutingArgument
{
    public SocketCommandContext Context { get; init; }
    public string CleanContent { get; init; }
    public string CommandUsed { get; init; }
    public string[]? Arguments { get; init; }
    public ILogger Logger { get; init; }
    public DirectoryInfo PluginBaseDirectory { get; init; }

    public DbCommandExecutingArgument(ILogger logger, SocketCommandContext context, string cleanContent, string commandUsed, string[]? arguments, DirectoryInfo pluginBaseDirectory)
    {
        this.Logger       = logger;
        this.Context      = context;
        this.CleanContent = cleanContent;
        this.CommandUsed  = commandUsed;
        this.Arguments    = arguments;
        this.PluginBaseDirectory = pluginBaseDirectory;
    }
    
}