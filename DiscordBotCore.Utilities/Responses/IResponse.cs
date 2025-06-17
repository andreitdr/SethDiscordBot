namespace DiscordBotCore.Utilities.Responses;

public interface IResponse<out T>
{
    public bool IsSuccess { get; }
    public string Message { get; }
    public T? Data { get; }
}