using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace DiscordBotCore.Interfaces;

public interface IDbSlashCommand
{
    string Name { get; }
    string Description { get; }
    bool CanUseDm { get; }
    bool HasInteraction { get; }

    List<SlashCommandOptionBuilder> Options { get; }

    void ExecuteServer(SocketSlashCommand context)
    { }

    void ExecuteDm(SocketSlashCommand context) { }

    Task ExecuteInteraction(SocketInteraction interaction) => Task.CompletedTask;
}
