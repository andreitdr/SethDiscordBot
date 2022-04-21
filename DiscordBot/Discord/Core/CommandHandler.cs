using Discord.Commands;
using Discord.WebSocket;

using PluginManager.Interfaces;

using System.Reflection;
using PluginManager.Others;
using PluginManager.Others.Permissions;
using PluginManager.Loaders;

using System.Threading.Tasks;
using System.Linq;
using Discord;
using System;

namespace PluginManager.Core
{
    internal class CommandHandler
    {
        private readonly DiscordSocketClient client;
        private readonly CommandService commandService;
        private readonly string botPrefix;

        internal static bool awaitRestartOnSetCommand = false;
        internal static SocketUser RestartOnSetCommandCaster = null;

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
                    if (message.Author.IsBot) return;
                    else
                    {
                        if (awaitRestartOnSetCommand && RestartOnSetCommandCaster is not null)
                        {
                            if (message.Content.ToLower() == "yes")
                            {
                                if (!(((SocketGuildUser)message.Author).hasPermission(GuildPermission.Administrator)))
                                {
                                    await message.Channel.SendMessageAsync("You do not have permission to use this command !");
                                    awaitRestartOnSetCommand = false;
                                    RestartOnSetCommandCaster = null;
                                    return;
                                }
                                var fileName = Assembly.GetExecutingAssembly().Location;
                                System.Diagnostics.Process.Start(fileName);
                                Environment.Exit(0);
                            }
                        }
                        return;
                    }

                var context = new SocketCommandContext(client, message);

                await commandService.ExecuteAsync(
                    context: context,
                    argPos: argPos,
                    services: null
                );

                DBCommand plugin = PluginLoader.Plugins!.Where(p => p.Command == (message.Content.Split(' ')[0]).Substring(botPrefix.Length)).FirstOrDefault();


                if (plugin != null)
                {
                    if (message.Channel == await message.Author.CreateDMChannelAsync())
                    {
                        if (plugin.canUseDM)
                        {
                            if (plugin.requireAdmin)
                            {
                                if (message.Author.isAdmin())
                                {
                                    plugin.Execute(context, message, client, true);
                                    Functions.WriteLogFile($"[{message.Author.Id}] Executed command (DM) : " + plugin.Command);
                                    return;
                                }
                                await message.Channel.SendMessageAsync("This command is for administrators only !");
                                return;
                            }
                            plugin.Execute(context, message, client, true);
                            Functions.WriteLogFile($"[{message.Author.Id}] Executed command (DM) : " + plugin.Command);
                            return;
                        }

                        await message.Channel.SendMessageAsync("This command is not for DMs");
                        return;
                    }
                    if (plugin.canUseServer)
                    {
                        if (plugin.requireAdmin)
                        {
                            if (message.Author.isAdmin())
                            {
                                plugin.Execute(context, message, client, false);
                                Functions.WriteLogFile($"[{message.Author.Id}] Executed command : " + plugin.Command);
                                return;
                            }
                            await message.Channel.SendMessageAsync("This command is for administrators only !");
                            return;
                        }
                        plugin.Execute(context, message, client, false);
                        Functions.WriteLogFile($"[{message.Author.Id}] Executed command : " + plugin.Command);
                        return;
                    }
                    return;

                }
            }
            catch { }

        }
    }
}