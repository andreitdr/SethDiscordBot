using Discord.Commands;
using Discord.WebSocket;


namespace PluginManager.Others;

public class DbCommandExecutingArguments
{
    public DbCommandExecutingArguments(
        SocketCommandContext context, string cleanContent, string commandUsed, string[]? arguments)
    {
        this.context      = context;
        this.cleanContent = cleanContent;
        this.commandUsed  = commandUsed;
        this.arguments    = arguments;
    }

    public DbCommandExecutingArguments(SocketUserMessage? message, DiscordSocketClient client)
    {
        context = new SocketCommandContext(client, message);
        var pos = 0;
        if (message.HasMentionPrefix(client.CurrentUser, ref pos))
        {
            var mentionPrefix = "<@" + client.CurrentUser.Id + ">";
            cleanContent = message.Content.Substring(mentionPrefix.Length + 1);
        }
        else
        {
            cleanContent = message.Content.Substring(Config.DiscordBot.botPrefix.Length);
        }

        var split = cleanContent.Split(' ');

        string[]? argsClean = null;
        if (split.Length > 1)
            argsClean = string.Join(' ', split, 1, split.Length - 1).Split(' ');

        commandUsed = split[0];
        arguments   = argsClean;
    }

    public SocketCommandContext context { get; init; }
    public string cleanContent { get; init; }
    public string commandUsed { get; init; }
    public string[]? arguments { get; init; }
    public ISocketMessageChannel Channel => context.Channel;
}
