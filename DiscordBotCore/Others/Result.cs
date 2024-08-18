using System;

namespace DiscordBotCore.Others;

public class Result
{
    private bool? _Result;
    private Exception? Exception { get; }
    
    
    private Result(Exception exception)
    {
        _Result = null;
        Exception = exception;
    }
    
    private Result(bool result)
    {
        _Result   = result;
        Exception = null;
    }
    
    public static Result Success() => new Result(true);
    public static Result Failure(Exception ex) => new Result(ex);
    public static Result Failure(string message) => new Result(new Exception(message));
    
    public void Match(Action successAction, Action<Exception> exceptionAction)
    {
        if (_Result.HasValue && _Result.Value)
        {
            successAction();
        }
        else
        {
            exceptionAction(Exception!);
        }
    }
    
}

public class Result<T>
{
    private readonly OneOf<T, Exception> _Result;
    
    private Result(OneOf<T, Exception> result)
    {
        _Result = result;
    }
    
    public static Result<T> From (T value) => new Result<T>(new OneOf<T, Exception>(value));
    public static implicit operator Result<T>(Exception exception) => new Result<T>(new OneOf<T, Exception>(exception));
    
    
    
    public void Match(Action<T> valueAction, Action<Exception> exceptionAction)
    {
        _Result.Match(valueAction, exceptionAction);
    }
    
    public TResult Match<TResult>(Func<T, TResult> valueFunc, Func<Exception, TResult> exceptionFunc)
    {
        return _Result.Match(valueFunc, exceptionFunc);
    }
}
