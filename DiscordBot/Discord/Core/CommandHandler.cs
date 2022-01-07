using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

using System.Reflection;
using PluginManager.Others;
using PluginManager.Loaders;

using System.Threading.Tasks;
using System.Linq;

namespace PluginManager.Core
{
    internal class CommandHandler
    {
        private readonly DiscordSocketClient client;
        private readonly CommandService commandService;
        private readonly string botPrefix;

        public CommandHandler(DiscordSocketClient client, CommandService commandService, string botPrefix)
        {
            this.client = client;
            this.commandService = commandService;
            this.botPrefix = botPrefix;
        }

        public async Task InstallCommandsAsync()
        {
            client.MessageReceived += MessageHandler;
            await commandService.AddModulesAsync(assembly: Assembly.GetEntryAssembly(), services: null);
        }

        private async Task MessageHandler(SocketMessage Message)
        {
            try
            {
                if (Message as SocketUserMessage == null)
                    return;

                var message = Message as SocketUserMessage;

                if (message == null) return;

                int argPos = 0;

                if (message.HasMentionPrefix(client.CurrentUser, ref argPos))
                {
                    await message.Channel.SendMessageAsync("Can not exec mentioned commands !");
                    return;
                }

                if (!(message.HasStringPrefix(botPrefix, ref argPos) || message.Author.IsBot))
                    return;

                var context = new SocketCommandContext(client, message);

                await commandService.ExecuteAsync(
                    context: context,
                    argPos: argPos,
                    services: null
                );

                DBCommand? plugin = PluginLoader.Plugins!.Where(p => p.Command == (message.Content.Split(' ')[0]).Substring(botPrefix.Length)).FirstOrDefault();

                if (plugin != null)
                {
                    if (message.Channel == await message.Author.CreateDMChannelAsync())
                    {
                        if (plugin.canUseDM)
                        {
                            plugin.Execute(context, message, client, true);
                            Functions.WriteLogFile("Executed command (DM) : " + plugin.Command);
                        }
                        return;
                    }
                    plugin.Execute(context, message, client, false);
                    Functions.WriteLogFile("Executed command : " + plugin.Command);
                }
            }
            catch { }

        }
    }
}