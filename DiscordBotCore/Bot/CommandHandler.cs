using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Discord.Commands;
using Discord.WebSocket;
using DiscordBotCore.Configuration;
using DiscordBotCore.Logging;
using DiscordBotCore.Others;
using DiscordBotCore.PluginCore.Helpers;
using DiscordBotCore.PluginCore.Helpers.Execution.DbCommand;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.PluginManagement.Loading;

namespace DiscordBotCore.Bot;

internal class CommandHandler : ICommandHandler
{
    private readonly string                _botPrefix;
    private readonly CommandService        _commandService;
    private readonly ILogger               _logger;
    private readonly IPluginLoader        _pluginLoader;
    private readonly IConfiguration       _configuration;

    /// <summary>
    ///     Command handler constructor
    /// </summary>
    /// <param name="pluginLoader">The plugin loader</param>
    /// <param name="commandService">The discord bot command service</param>
    /// <param name="botPrefix">The prefix to watch for</param>
    /// <param name="logger">The logger</param>
    public CommandHandler(ILogger logger, IPluginLoader pluginLoader, IConfiguration configuration, CommandService commandService, string botPrefix)
    {
        _commandService = commandService;
        _botPrefix      = botPrefix;
        _logger         = logger;
        _pluginLoader    = pluginLoader;
        _configuration  = configuration;
    }

    /// <summary>
    ///     The method to initialize all commands
    /// </summary>
    /// <returns></returns>
    public async Task InstallCommandsAsync(DiscordSocketClient client)
    {
        client.MessageReceived      += (message) => MessageHandler(client, message);
        client.SlashCommandExecuted += Client_SlashCommandExecuted;
        await _commandService.AddModulesAsync(Assembly.GetEntryAssembly(), null);
    }

    private Task Client_SlashCommandExecuted(SocketSlashCommand arg)
    {
        try
        {
            var plugin = _pluginLoader.SlashCommands.FirstOrDefault(p => p.Name == arg.Data.Name);

            if (plugin is null)
                throw new Exception("Failed to run command !");

            if (arg.Channel is SocketDMChannel)
                plugin.ExecuteDm(_logger, arg);
            else plugin.ExecuteServer(_logger, arg);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, this);
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     The message handler for the bot
    /// </summary>
    /// <param name="Message">The message got from the user in discord chat</param>
    /// <returns></returns>
    private async Task MessageHandler(DiscordSocketClient socketClient, SocketMessage socketMessage)
    {
        try
        {
            if (socketMessage.Author.IsBot)
                return;

            if (socketMessage as SocketUserMessage == null)
                return;

            var message = socketMessage as SocketUserMessage;

            if (message is null)
                return;

            var argPos = 0;

            if (!message.Content.StartsWith(_botPrefix) && !message.HasMentionPrefix(socketClient.CurrentUser, ref argPos))
                return;

            var context = new SocketCommandContext(socketClient, message);

            await _commandService.ExecuteAsync(context, argPos, null);

            IDbCommand? plugin;
            var        cleanMessage = "";

            if (message.HasMentionPrefix(socketClient.CurrentUser, ref argPos))
            {
                var mentionPrefix = "<@" + socketClient.CurrentUser.Id + ">";

                plugin = _pluginLoader.Commands!
                                     .FirstOrDefault(plug => plug.Command ==
                                                             message.Content.Substring(mentionPrefix.Length + 1)
                                                                    .Split(' ')[0] ||
                                                             plug.Aliases is not null &&
                                                             plug.Aliases.Contains(message.CleanContent
                                                                                          .Substring(mentionPrefix.Length + 1)
                                                                                          .Split(' ')[0]
                                                             )
                                     );

                cleanMessage = message.Content.Substring(mentionPrefix.Length + 1);
            }

            else
            {
                plugin = _pluginLoader.Commands!
                                     .FirstOrDefault(p => p.Command ==
                                                          message.Content.Split(' ')[0].Substring(_botPrefix.Length) ||
                                                          p.Aliases is not null &&
                                                          p.Aliases.Contains(
                                                              message.Content.Split(' ')[0]
                                                                     .Substring(_botPrefix.Length)
                                                          )
                                     );
                cleanMessage = message.Content.Substring(_botPrefix.Length);
            }

            if (plugin is null)
                return;

            if (plugin.RequireAdmin && !context.Message.Author.IsAdmin())
                return;

            var split = cleanMessage.Split(' ');

            string[]? argsClean = null;
            if (split.Length > 1)
                argsClean = string.Join(' ', split, 1, split.Length - 1).Split(' ');

            DbCommandExecutingArgument cmd = new(_logger,
                context,
                cleanMessage,
                split[0],
                argsClean,
                new DirectoryInfo(Path.Combine(_configuration.Get<string>("ResourcesFolder"), plugin.Command)));

            _logger.Log(
                $"User ({context.User.Username}) from Guild \"{context.Guild.Name}\" executed command \"{cmd.CleanContent}\"",
                this,
                LogType.Info
            );

            if (context.Channel is SocketDMChannel)
                await plugin.ExecuteDm(cmd);
            else await plugin.ExecuteServer(cmd);
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, this);
        }
    }
}
