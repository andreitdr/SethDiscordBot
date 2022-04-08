using Discord;
using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Loaders;
using PluginManager.Interfaces;
using PluginManager.Others.Permissions;
using PluginManager.Others;

using System.Collections.Generic;

namespace PluginManager.Commands
{
    internal class Help : DBCommand
    {
        public string Command => "help";

        public string Description => "This command allows you to check all loadded commands";

        public string Usage => "help";

        public bool canUseDM => true;
        public bool canUseServer => true;

        public bool requireAdmin => false;

        public void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            List<string> args = Functions.GetArguments(message);
            if (args.Count != 0)
            {

                foreach (var item in args)
                {
                    bool commandExists = false;
                    var e = GenerateHelpCommand(item);
                    if (e != null)
                    {
                        commandExists = true;
                        context.Channel.SendMessageAsync(embed: e.Build());
                    }
                    if (!commandExists)
                        context.Channel.SendMessageAsync("Unknown Command " + item);
                }
                return;
            }

            bool isAdmin = ((SocketGuildUser)message.Author).isAdmin();
            Discord.EmbedBuilder embedBuilder = new Discord.EmbedBuilder();

            string adminCommands = "";
            string normalCommands = "";
            string DMCommands = "";

            foreach (var cmd in PluginLoader.Plugins!)
            {
                if (cmd.canUseDM)
                    DMCommands += cmd.Command + " ";
                if (cmd.requireAdmin)
                    adminCommands += cmd.Command + " ";
                else normalCommands += cmd.Command + " ";
            }

            embedBuilder.AddField("Admin Commands", adminCommands);
            embedBuilder.AddField("Normal Commands", normalCommands);
            embedBuilder.AddField("DM Commands", DMCommands);
            context.Channel.SendMessageAsync(embed: embedBuilder.Build());

        }

        private EmbedBuilder GenerateHelpCommand(string command)
        {
            EmbedBuilder embedBuilder = new EmbedBuilder();
            DBCommand cmd = PluginLoader.Plugins.Find(p => p.Command == command);
            if (cmd == null)
                return null;

            embedBuilder.AddField("Usage", cmd.Usage);
            embedBuilder.AddField("Description", cmd.Description);

            return embedBuilder;
        }
    }
}