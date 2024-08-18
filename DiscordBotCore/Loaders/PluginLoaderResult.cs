using System;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;

namespace DiscordBotCore.Loaders;


public class PluginLoaderResult
{
    private Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception> _Result;

    public static PluginLoaderResult FromIDbCommand(IDbCommand command) => new PluginLoaderResult(new Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception>(command));

    public static PluginLoaderResult FromIDbEvent(IDbEvent dbEvent) => new PluginLoaderResult(new Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception>(dbEvent));

    public static PluginLoaderResult FromIDbSlashCommand(IDbSlashCommand slashCommand) => new PluginLoaderResult(new Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception>(slashCommand));

    public static PluginLoaderResult FromICommandAction(ICommandAction commandAction) => new PluginLoaderResult(new Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception>(commandAction));

    public static PluginLoaderResult FromException(Exception exception) => new PluginLoaderResult(new Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception>(exception));
    public static PluginLoaderResult FromException(string exception) => new PluginLoaderResult(new Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception>(new Exception(message: exception)));
    
    private PluginLoaderResult(Option4<IDbCommand, IDbEvent, IDbSlashCommand, ICommandAction, Exception> result)
    {
        _Result = result;
    }
    
    public void Match(Action<IDbCommand> commandAction, Action<IDbEvent> eventAction, Action<IDbSlashCommand> slashCommandAction,
        Action<ICommandAction> commandActionAction, Action<Exception> exceptionAction)
    {
        _Result.Match(commandAction, eventAction, slashCommandAction, commandActionAction, exceptionAction);
    }

    public TResult Match<TResult>(Func<IDbCommand, TResult> commandFunc, Func<IDbEvent, TResult> eventFunc,
        Func<IDbSlashCommand, TResult> slashCommandFunc, Func<ICommandAction, TResult> commandActionFunc,
        Func<Exception, TResult> exceptionFunc)
    {
        return _Result.Match(commandFunc, eventFunc, slashCommandFunc, commandActionFunc, exceptionFunc);
    }

}