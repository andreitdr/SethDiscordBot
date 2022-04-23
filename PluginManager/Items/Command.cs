using Discord.WebSocket;

using PluginManager.Loaders;
using PluginManager.Others;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Items
{
    internal class Command
    {
        public SocketUser Author;
        public List<string> Arguments { get; private set; }
        public string CommandName { get; private set; }
        public char PrefixUsed { get; private set; }
        public Command(SocketMessage message)
        {
            this.Author = message.Author;
            string[] data = message.Content.Split(' ');
            if (data.Length > 1)
                this.Arguments = new List<string>(data.MergeStrings(1).Split(' '));
            else this.Arguments = new List<string>();
            this.CommandName = data[0].Substring(1);
            this.PrefixUsed = data[0][0];
        }

        public Command(string message, bool hasPrefix)
        {
            string[] data = message.Split(' ');

            this.Author = null;
            this.Arguments = new List<string>(data.MergeStrings(1).Split(' '));
            this.CommandName = data[0].Substring(1);
            if (hasPrefix)
                this.PrefixUsed = data[0][0];
            else this.PrefixUsed = '\0'; //null
        }

    }
}
