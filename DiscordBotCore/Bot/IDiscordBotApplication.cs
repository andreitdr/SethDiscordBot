using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBotCore.Bot;

public interface IDiscordBotApplication
{
    public DiscordSocketClient Client { get; }
    
    /// <summary>
    ///     The start method for the bot. This method is used to load the bot
    /// </summary>
    Task StartAsync();
}