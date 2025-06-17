using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBotCore.Bot;

public interface IDiscordBotApplication
{
    public bool IsReady { get; }
    public DiscordSocketClient Client { get; }
    
    /// <summary>
    ///     The start method for the bot. This method is used to load the bot
    /// </summary>
    Task StartAsync();

    /// <summary>
    /// Stops the bot and cleans up resources.
    /// </summary>
    Task StopAsync();
}