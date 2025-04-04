using DiscordBotCore.PluginCore;
using DiscordBotCore.PluginCore.Interfaces;
using DiscordBotCore.Utilities;

namespace DiscordBotCore.PluginManagement.Loading;


public class PluginLoaderResult
{
    private Option3<IDbCommand, IDbEvent, IDbSlashCommand, Exception> _Result;

    public static PluginLoaderResult FromIDbCommand(IDbCommand command) => new PluginLoaderResult(new Option3<IDbCommand, IDbEvent, IDbSlashCommand, Exception>(command));

    public static PluginLoaderResult FromIDbEvent(IDbEvent dbEvent) => new PluginLoaderResult(new Option3<IDbCommand, IDbEvent, IDbSlashCommand, Exception>(dbEvent));

    public static PluginLoaderResult FromIDbSlashCommand(IDbSlashCommand slashCommand) => new PluginLoaderResult(new Option3<IDbCommand, IDbEvent, IDbSlashCommand, Exception>(slashCommand));

    public static PluginLoaderResult FromException(Exception exception) => new PluginLoaderResult(new Option3<IDbCommand, IDbEvent, IDbSlashCommand, Exception>(exception));
    private PluginLoaderResult(Option3<IDbCommand, IDbEvent, IDbSlashCommand, Exception> result)
    {
        _Result = result;
    }
    
    public void Match(Action<IDbCommand> commandAction, Action<IDbEvent> eventAction, Action<IDbSlashCommand> slashCommandAction, 
        Action<Exception> exceptionAction)
    {
        _Result.Match(commandAction, eventAction, slashCommandAction, exceptionAction);
    }

    public TResult Match<TResult>(Func<IDbCommand, TResult> commandFunc, Func<IDbEvent, TResult> eventFunc,
        Func<IDbSlashCommand, TResult> slashCommandFunc,
        Func<Exception, TResult> exceptionFunc)
    {
        return _Result.Match(commandFunc, eventFunc, slashCommandFunc, exceptionFunc);
    }

}