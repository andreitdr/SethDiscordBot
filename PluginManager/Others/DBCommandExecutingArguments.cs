using Discord.Commands;

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

    public SocketCommandContext context      { get; init; }
    public string               cleanContent { get; init; }
    public string               commandUsed  { get; init; }
    public string[]?            arguments    { get; init; }
}
