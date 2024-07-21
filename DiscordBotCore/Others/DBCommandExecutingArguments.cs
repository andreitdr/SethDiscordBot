using Discord.Commands;
using Discord.WebSocket;


namespace DiscordBotCore.Others;

public class DbCommandExecutingArguments
{

    public SocketCommandContext Context { get; init; }
    public string CleanContent { get; init; }
    public string CommandUsed { get; init; }
    public string[]? Arguments { get; init; }
    public ISocketMessageChannel Channel => Context.Channel;

    public DbCommandExecutingArguments(
        SocketCommandContext context, string cleanContent, string commandUsed, string[]? arguments)
    {
        this.Context      = context;
        this.CleanContent = cleanContent;
        this.CommandUsed  = commandUsed;
        this.Arguments    = arguments;
    }

    public DbCommandExecutingArguments(SocketUserMessage? message, DiscordSocketClient client)
    {
        Context = new SocketCommandContext(client, message);
        var pos = 0;
        if (message.HasMentionPrefix(client.CurrentUser, ref pos))
        {
            var mentionPrefix = "<@" + client.CurrentUser.Id + ">";
            CleanContent = message.Content.Substring(mentionPrefix.Length + 1);
        }
        else
        {
            CleanContent = message.Content.Substring(Application.CurrentApplication.DiscordBotClient.BotPrefix.Length);
        }

        var split = CleanContent.Split(' ');

        string[]? argsClean = null;
        if (split.Length > 1)
            argsClean = string.Join(' ', split, 1, split.Length - 1).Split(' ');

        CommandUsed = split[0];
        Arguments   = argsClean;
    }
}
