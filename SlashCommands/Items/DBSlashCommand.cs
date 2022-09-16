using Discord.WebSocket;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SlashCommands.Items
{
    public interface DBSlashCommand
    {
        string Command { get; }
        string Description { get; }
        string Usage { get; }
        bool requireAdmin { get; }
        bool PrivateResponse { get; }
        Task ExecuteServer(SocketSlashCommand command);
        Task InitializeCommand(DiscordSocketClient client);
    }
}
