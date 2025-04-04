using Discord;
using Discord.WebSocket;
using DiscordBotCore.Logging;

namespace DiscordBotCore.PluginCore.Interfaces;

public interface IDbSlashCommand
{
    string Name { get; }
    string Description { get; }
    bool CanUseDm { get; }
    bool HasInteraction { get; }

    List<SlashCommandOptionBuilder> Options { get; }

    void ExecuteServer(ILogger logger, SocketSlashCommand context)
    { }

    void ExecuteDm(ILogger logger, SocketSlashCommand context) { }

    Task ExecuteInteraction(ILogger logger, SocketInteraction interaction) => Task.CompletedTask;
}
