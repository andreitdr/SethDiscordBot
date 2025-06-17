namespace DiscordBotCore.Utilities;
public class Option2<T0, T1, TError> where TError : Exception
{
    private readonly int _Index;
    
    private T0 Item0 { get; } = default!;
    private T1 Item1 { get; } = default!;
    
    private TError Error { get; } = default!;
    
    public Option2(T0 item0)
    {
        Item0 = item0;
        _Index = 0;
    }
    
    public Option2(T1 item1)
    {
        Item1 = item1;
        _Index = 1;
    }
    
    public Option2(TError error)
    {
        Error = error;
        _Index = 2;
    }
    
    public static implicit operator Option2<T0, T1, TError>(T0 item0) => new Option2<T0, T1, TError>(item0);
    public static implicit operator Option2<T0, T1, TError>(T1 item1) => new Option2<T0, T1, TError>(item1);    
    public static implicit operator Option2<T0, T1, TError>(TError error) => new Option2<T0, T1, TError>(error);
    
    public void Match(Action<T0> item0, Action<T1> item1, Action<TError> error)
    {
        switch (_Index)
        {
            case 0:
                item0(Item0);
                break;
            case 1:
                item1(Item1);
                break;
            case 2:
                error(Error);
                break;
            default:
                throw new InvalidOperationException();
        }
    }
    
    public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<TError, TResult> error)
    {
        return _Index switch
        {
            0 => item0(Item0),
            1 => item1(Item1),
            2 => error(Error),
            _ => throw new InvalidOperationException(),
        };
    }
    
    public override string ToString()
    {
        return _Index switch
        {
            0 => $"Option2<{typeof(T0).Name}>: {Item0}",
            1 => $"Option2<{typeof(T1).Name}>: {Item1}",
            2 => $"Option2<{typeof(TError).Name}>: {Error}",
            _ => "Invalid Option2"
        };
    }
    
}

public class Option3<T0, T1, T2, TError> where TError : Exception
{
    private readonly int _Index;
    
    private T0 Item0 { get; } = default!;
    private T1 Item1 { get; } = default!;
    private T2 Item2 { get; } = default!;
    private TError Error { get; } = default!;
    
    public Option3(T0 item0)
    {
        Item0 = item0;
        _Index = 0;
    }
    
    public Option3(T1 item1)
    {
        Item1 = item1;
        _Index = 1;
    }
    
    public Option3(T2 item2)
    {
        Item2 = item2;
        _Index = 2;
    }
    
    public Option3(TError error)
    {
        Error = error;
        _Index = 3;
    }
    
    public static implicit operator Option3<T0, T1, T2, TError>(T0 item0) => new Option3<T0, T1, T2, TError>(item0);
    public static implicit operator Option3<T0, T1, T2, TError>(T1 item1) => new Option3<T0, T1, T2, TError>(item1);
    public static implicit operator Option3<T0, T1, T2, TError>(T2 item2) => new Option3<T0, T1, T2, TError>(item2);
    public static implicit operator Option3<T0, T1, T2, TError>(TError error) => new Option3<T0, T1, T2, TError>(error);
    
    public void Match(Action<T0> item0, Action<T1> item1, Action<T2> item2, Action<TError> error)
    {
        switch (_Index)
        {
            case 0:
                item0(Item0);
                break;
            case 1:
                item1(Item1);
                break;
            case 2:
                item2(Item2);
                break;
            case 3:
                error(Error);
                break;
            default:
                throw new InvalidOperationException();
        }
    }
    
    public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<T2, TResult> item2, Func<TError, TResult> error)
    {
        return _Index switch
        {
            0 => item0(Item0),
            1 => item1(Item1),
            2 => item2(Item2),
            3 => error(Error),
            _ => throw new InvalidOperationException(),
        };
    }
    
    public override string ToString()
    {
        return _Index switch
        {
            0 => $"Option3<{typeof(T0).Name}>: {Item0}",
            1 => $"Option3<{typeof(T1).Name}>: {Item1}",
            2 => $"Option3<{typeof(T2).Name}>: {Item2}",
            3 => $"Option3<{typeof(TError).Name}>: {Error}",
            _ => "Invalid Option3"
        };
    }
}

public class Option4<T0, T1, T2, T3, TError> where TError : Exception
{
    private readonly int _Index;

    private T0 Item0 { get; } = default!;
    private T1 Item1 { get; } = default!;
    private T2 Item2 { get; } = default!;
    private T3 Item3 { get; } = default!;
    
    private TError Error { get; } = default!;

    public Option4(T0 item0)
    {
        Item0 = item0;
        _Index = 0;
    }

    public Option4(T1 item1)
    {
        Item1 = item1;
        _Index = 1;
    }

    public Option4(T2 item2)
    {
        Item2 = item2;
        _Index = 2;
    }

    public Option4(T3 item3)
    {
        Item3 = item3;
        _Index = 3;
    }

    public Option4(TError error)
    {
        Error = error;
        _Index = 4;
    }


    public static implicit operator Option4<T0, T1, T2, T3, TError>(T0 item0) => new Option4<T0, T1, T2, T3, TError>(item0);
    public static implicit operator Option4<T0, T1, T2, T3, TError>(T1 item1) => new Option4<T0, T1, T2, T3, TError>(item1);
    public static implicit operator Option4<T0, T1, T2, T3, TError>(T2 item2) => new Option4<T0, T1, T2, T3, TError>(item2);
    public static implicit operator Option4<T0, T1, T2, T3, TError>(T3 item3) => new Option4<T0, T1, T2, T3, TError>(item3);
    public static implicit operator Option4<T0, T1, T2, T3, TError>(TError error) => new Option4<T0, T1, T2, T3, TError>(error);
    

    public void Match(Action<T0> item0, Action<T1> item1, Action<T2> item2, Action<T3> item3, Action<TError> error)
    {
        switch (_Index)
        {
            case 0:
                item0(Item0);
                break;
            case 1:
                item1(Item1);
                break;
            case 2:
                item2(Item2);
                break;
            case 3:
                item3(Item3);
                break;
            case 4:
                error(Error);
                break;
            default:
                throw new InvalidOperationException();
        }
    }
  

    public TResult Match<TResult>(Func<T0, TResult> item0, Func<T1, TResult> item1, Func<T2, TResult> item2, Func<T3, TResult> item3, Func<TError, TResult> error)
    {
        return _Index switch
        {
            0 => item0(Item0),
            1 => item1(Item1),
            2 => item2(Item2),
            3 => item3(Item3),
            4 => error(Error),
            _ => throw new InvalidOperationException(),
        };
    }

    public override string ToString()
    {
        return _Index switch
        {
            0 => $"Option4<{typeof(T0).Name}>: {Item0}",
            1 => $"Option4<{typeof(T1).Name}>: {Item1}",
            2 => $"Option4<{typeof(T2).Name}>: {Item2}",
            3 => $"Option4<{typeof(T3).Name}>: {Item3}",
            4 => $"Option4<{typeof(TError).Name}>: {Error}",
            _ => "Invalid Option4"
        };
    }
}