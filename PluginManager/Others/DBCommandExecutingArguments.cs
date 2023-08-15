using System.Linq;
using Discord.Commands;
using Discord.WebSocket;
using PluginManager.Bot;

namespace PluginManager.Others;

public class DBCommandExecutingArguments
{
    public DBCommandExecutingArguments(
        SocketCommandContext context, string cleanContent, string commandUsed, string[]? arguments)
    {
        this.context      = context;
        this.cleanContent = cleanContent;
        this.commandUsed  = commandUsed;
        this.arguments    = arguments;
    }

    public DBCommandExecutingArguments(SocketUserMessage? message, DiscordSocketClient client)
    {
        this.context = new SocketCommandContext(client, message);
        int pos = 0;
        if (message.HasMentionPrefix(client.CurrentUser, ref pos))
        {
            var mentionPrefix = "<@" + client.CurrentUser.Id + ">";
            this.cleanContent = message.Content.Substring(mentionPrefix.Length + 1);
        }
        else
        {
            this.cleanContent = message.Content.Substring(Config.DiscordBot.botPrefix.Length);
        }
        
        var split = this.cleanContent.Split(' ');

        string[]? argsClean = null;
        if (split.Length > 1)
            argsClean = string.Join(' ', split, 1, split.Length - 1).Split(' ');
        
        this.commandUsed = split[0];
        this.arguments = argsClean;
    }

    public SocketCommandContext context      { get; init; }
    public string               cleanContent { get; init; }
    public string               commandUsed  { get; init; }
    public string[]?            arguments    { get; init; }
}
