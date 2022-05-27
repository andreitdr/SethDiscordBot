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
        /// <summary>
        /// The author of the command
        /// </summary>
        public SocketUser? Author;

        /// <summary>
        /// The list of arguments
        /// </summary>
        public List<string> Arguments { get; private set; }

        /// <summary>
        /// The command that is executed
        /// </summary>
        public string CommandName { get; private set; }

        /// <summary>
        /// The prefix that is used for the command
        /// </summary>
        public char PrefixUsed { get; private set; }

        /// <summary>
        /// The Command class contructor
        /// </summary>
        /// <param name="message">The message that was sent</param>
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
    }
}
