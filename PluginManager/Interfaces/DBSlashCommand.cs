using System.Collections.Generic;

using Discord;
using Discord.WebSocket;

namespace PluginManager.Interfaces
{
    public interface DBSlashCommand
    {
        string Name { get; }
        string Description { get; }

        bool canUseDM { get; }

        List<SlashCommandOptionBuilder> Options { get; }

        void ExecuteServer(SocketSlashCommand context)
        {

        }

        void ExecuteDM(SocketSlashCommand context) { }

    }
}
