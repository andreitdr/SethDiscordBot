using System;
using System.Collections.Generic;
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
    private static readonly string _DefaultPrefix = ";";
    
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
    public CommandHandler(ILogger logger, IPluginLoader pluginLoader, IConfiguration configuration, CommandService commandService)
    {
        _commandService = commandService;
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
            {
                _logger.Log($"Received slash command /{arg.Data.Name}, but no loaded plugin handles it.", this, LogType.Warning);
                return Task.CompletedTask;
            }

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

            string botPrefix = this._configuration.Get<string>("prefix", _DefaultPrefix);

            // HasMentionPrefix accepts both <@id> and <@!id> and sets argPos to the first character after the mention
            bool isMention = message.HasMentionPrefix(socketClient.CurrentUser, ref argPos);
            if (!isMention && !message.Content.StartsWith(botPrefix))
                return;

            var context = new SocketCommandContext(socketClient, message);

            await _commandService.ExecuteAsync(context, argPos, null);

            ParsedCommand? parsed = ParseCommand(message.Content, botPrefix, isMention ? argPos : null, _pluginLoader.Commands);
            if (parsed is null)
            {
                return;
            }

            IDbCommand plugin = parsed.Command;

            bool isDm = context.Channel is SocketDMChannel;

            // Admin rights only exist inside a guild, so admin commands never run in DMs
            if (plugin.RequireAdmin && (isDm || !context.Message.Author.IsAdmin()))
            {
                return;
            }

            DbCommandExecutingArgument cmd = new(_logger,
                context,
                parsed.CleanContent,
                parsed.CommandUsed,
                parsed.Arguments,
                new DirectoryInfo(Path.Combine(_configuration.Get<string>("ResourcesFolder"), plugin.Command)));

            string source = isDm ? "DM" : $"Guild \"{context.Guild.Name}\"";
            _logger.Log(
                $"User ({context.User.Username}) from {source} executed command \"{cmd.CleanContent}\"",
                this,
                LogType.Info
            );

            if (isDm)
            {
                await plugin.ExecuteDm(cmd);
            }
            else
            {
                await plugin.ExecuteServer(cmd);
            }
        }
        catch (Exception ex)
        {
            _logger.LogException(ex, this);
        }
    }

    /// <summary>
    ///     Finds the command a message asks for.
    /// </summary>
    /// <param name="content">The message text</param>
    /// <param name="prefix">The command prefix</param>
    /// <param name="mentionEnd">Where the text after a leading bot mention starts, or null if the message doesn't start with one</param>
    /// <param name="commands">The loaded commands, matched by name or alias (case-sensitive)</param>
    /// <returns>The command and its arguments, or null if the message isn't a known command</returns>
    internal static ParsedCommand? ParseCommand(string content, string prefix, int? mentionEnd, IEnumerable<IDbCommand> commands)
    {
        string cleanMessage;
        if (mentionEnd is int argPos)
        {
            cleanMessage = content.Substring(argPos).TrimStart();
        }
        else if (prefix.Length > 0 && content.StartsWith(prefix))
        {
            cleanMessage = content.Substring(prefix.Length);
        }
        else
        {
            return null;
        }

        string[] split = cleanMessage.Split(' ');
        string commandName = split[0];
        if (string.IsNullOrWhiteSpace(commandName))
        {
            return null;
        }

        IDbCommand? command = commands.FirstOrDefault(p => p.Command == commandName || p.Aliases.Contains(commandName));
        if (command is null)
        {
            return null;
        }

        return new ParsedCommand(command, cleanMessage, commandName, split.Length > 1 ? split[1..] : null);
    }
}

/// <param name="Command">The matched command</param>
/// <param name="CleanContent">The message without the prefix or mention</param>
/// <param name="CommandUsed">The name or alias that was typed</param>
/// <param name="Arguments">The words after the command, or null if there are none</param>
internal sealed record ParsedCommand(IDbCommand Command, string CleanContent, string CommandUsed, string[]? Arguments);
