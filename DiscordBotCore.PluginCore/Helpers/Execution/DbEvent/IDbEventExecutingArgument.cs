using Discord.WebSocket;
using DiscordBotCore.Logging;

namespace DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;

public interface IDbEventExecutingArgument
{
    public ILogger Logger { get; }
    public DiscordSocketClient Client { get; }
    public string BotPrefix { get; }
    public DirectoryInfo PluginBaseDirectory { get; }
}