namespace DiscordBotCore.Utilities.Responses;

public class Response : IResponse<bool>
{
    public bool IsSuccess => Data;
    public string Message { get; }
    public bool Data { get; }

    private Response(bool result)
    {
        Data = result;
        Message = string.Empty;
    }
    
    private Response(string message)
    {
        Data = false;
        Message = message;
    }
    
    public static Response Success() => new Response(true);
    public static Response Failure(string message) => new Response(message);
}

public class Response<T> : IResponse<T> where T : class
{
    public bool IsSuccess => Data is not null;
    public string Message { get; }
    public T? Data { get; }

    private Response(T data)
    {
        Data = data;
        Message = string.Empty;
    }
    
    private Response(string message)
    {
        Data = null;
        Message = message;
    }
    
    public static Response<T> Success(T data) => new Response<T>(data);
    public static Response<T> Failure(string message) => new Response<T>(message);
}