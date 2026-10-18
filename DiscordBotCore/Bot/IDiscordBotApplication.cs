using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBotCore.Bot;

public interface IDiscordBotApplication
{
    public bool IsReady { get; }
    public DiscordSocketClient Client { get; }
    
    /// <summary>
    ///     Logs in and waits until the client is Ready. Cancelling <paramref name="cancellationToken"/> abandons the
    ///     attempt, disconnects the client and throws <see cref="System.OperationCanceledException"/>.
    /// </summary>
    Task StartAsync(System.Threading.CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the bot and cleans up resources.
    /// </summary>
    Task StopAsync();
}