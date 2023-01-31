using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others
{
    public class CmdArgs
    {
        public SocketCommandContext context { get; init; }
        public string cleanContent { get; init; }
        public string commandUsed { get;init; }
        public string[] arguments { get;init; }

    }
}
