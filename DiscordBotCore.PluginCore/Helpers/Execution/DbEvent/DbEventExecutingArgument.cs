using Discord.WebSocket;
using DiscordBotCore.Logging;

namespace DiscordBotCore.PluginCore.Helpers.Execution.DbEvent;

public class DbEventExecutingArgument : IDbEventExecutingArgument
{
    public ILogger Logger { get; }
    public DiscordSocketClient Client { get; }
    public string BotPrefix { get; }
    public DirectoryInfo PluginBaseDirectory { get; }
    
    public DbEventExecutingArgument(ILogger logger, DiscordSocketClient client, string botPrefix, DirectoryInfo pluginBaseDirectory)
    {
        Logger     = logger;
        Client     = client;
        BotPrefix  = botPrefix;
        PluginBaseDirectory = pluginBaseDirectory;
    }
}