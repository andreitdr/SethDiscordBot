using Discord.Commands;
using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Others
{
    public class DBCommandExecutingArguments
    {
        public SocketCommandContext context { get; init; }
        public string cleanContent { get; init; }
        public string commandUsed { get;init; }
        public string[]? arguments { get;init; }

        public DBCommandExecutingArguments(SocketCommandContext context, string cleanContent, string commandUsed, string[] arguments)
        {
            this.context = context;
            this.cleanContent = cleanContent;
            this.commandUsed = commandUsed;
            this.arguments = arguments;
        }

    }
}
