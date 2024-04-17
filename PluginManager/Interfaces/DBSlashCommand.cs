using System.Collections.Generic;
using System.Threading.Tasks;

using Discord;
using Discord.WebSocket;

namespace PluginManager.Interfaces;

public interface DBSlashCommand
{
    string Name { get; }
    string Description { get; }
    bool canUseDM { get; }
    bool HasInteraction { get; }

    List<SlashCommandOptionBuilder> Options { get; }

    void ExecuteServer(SocketSlashCommand context)
    { }

    void ExecuteDM(SocketSlashCommand context) { }

    Task ExecuteInteraction(SocketInteraction interaction) => Task.CompletedTask;
}
