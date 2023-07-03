using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Interfaces;
using PluginManager.Loaders;
using PluginManager.Others;
using PluginManager.Others.Permissions;

namespace PluginManager.Bot;

internal class CommandHandler
{
    private readonly string              botPrefix;
    private readonly DiscordSocketClient client;
    private readonly CommandService      commandService;

    /// <summary>
    ///     Command handler constructor
    /// </summary>
    /// <param name="client">The discord bot client</param>
    /// <param name="commandService">The discord bot command service</param>
    /// <param name="botPrefix">The prefix to watch for</param>
    public CommandHandler(DiscordSocketClient client, CommandService commandService, string botPrefix)
    {
        this.client         = client;
        this.commandService = commandService;
        this.botPrefix      = botPrefix;
    }

    /// <summary>
    ///     The method to initialize all commands
    /// </summary>
    /// <returns></returns>
    public async Task InstallCommandsAsync()
    {
        client.MessageReceived      += MessageHandler;
        client.SlashCommandExecuted += Client_SlashCommandExecuted;
        await commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
    }

    private Task Client_SlashCommandExecuted(SocketSlashCommand arg)
    {
        try
        {
            var plugin = PluginLoader.SlashCommands!.FirstOrDefault(p => p.Name == arg.Data.Name);

            if (plugin is null)
                throw new Exception("Failed to run command. !");


            if (arg.Channel is SocketDMChannel)
                plugin.ExecuteDM(arg);
            else plugin.ExecuteServer(arg);
        }
        catch (Exception ex)
        {
            Config.Logger.Log(ex.Message, "CommandHandler", LogLevel.ERROR);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     The message handler for the bot
    /// </summary>
    /// <param name="Message">The message got from the user in discord chat</param>
    /// <returns></returns>
    private async Task MessageHandler(SocketMessage Message)
    {
        try
        {
            if (Message.Author.IsBot)
                return;

            if (Message as SocketUserMessage == null)
                return;

            var message = Message as SocketUserMessage;

            if (message is null)
                return;

            var argPos = 0;

            if (!message.Content.StartsWith(botPrefix) && !message.HasMentionPrefix(client.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(client, message);

            await commandService.ExecuteAsync(context, argPos, null);

            DBCommand? plugin;
            var        cleanMessage = "";

            if (message.HasMentionPrefix(client.CurrentUser, ref argPos))
            {
                var mentionPrefix = "<@" + client.CurrentUser.Id + ">";

                plugin = PluginLoader.Commands!
                                     .FirstOrDefault(plug => plug.Command ==
                                                             message.Content.Substring(mentionPrefix.Length + 1)
                                                                    .Split(' ')[0] ||
                                                             (
                                                                 plug.Aliases is not null &&
                                                                 plug.Aliases.Contains(message.CleanContent
                                                                     .Substring(mentionPrefix.Length + 1)
                                                                     .Split(' ')[0])
                                                             ));

                cleanMessage = message.Content.Substring(mentionPrefix.Length + 1);
            }

            else
            {
                plugin = PluginLoader.Commands!
                                     .FirstOrDefault(p => p.Command ==
                                                          message.Content.Split(' ')[0].Substring(botPrefix.Length) ||
                                                          (p.Aliases is not null &&
                                                           p.Aliases.Contains(
                                                                              message.Content.Split(' ')[0]
                                                                                  .Substring(botPrefix.Length))));
                cleanMessage = message.Content.Substring(botPrefix.Length);
            }

            if (plugin is null)
                throw new Exception("Failed to run command ! " + message.CleanContent + " (user: " +
                                    context.Message.Author.Username + " - " + context.Message.Author.Id + ")");

            if (plugin.requireAdmin && !context.Message.Author.isAdmin())
                return;

            var split = cleanMessage.Split(' ');

            string[]? argsClean = null;
            if (split.Length > 1)
                argsClean = string.Join(' ', split, 1, split.Length - 1).Split(' ');

            DBCommandExecutingArguments cmd = new(context, cleanMessage, split[0], argsClean);

            if (context.Channel is SocketDMChannel)
                plugin.ExecuteDM(cmd);
            else plugin.ExecuteServer(cmd);
        }
        catch (Exception ex)
        {
            Config.Logger.Log(ex.Message, this, LogLevel.ERROR);
        }
    }
}
