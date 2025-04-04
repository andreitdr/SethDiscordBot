using System.Threading.Tasks;
using Discord.WebSocket;

namespace DiscordBotCore.Bot;

internal interface ICommandHandler
{
    /// <summary>
    ///     The method to initialize all commands
    /// </summary>
    /// <returns></returns>
    Task InstallCommandsAsync(DiscordSocketClient client);
}