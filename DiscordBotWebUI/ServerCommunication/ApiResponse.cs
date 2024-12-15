using System.Text.Json.Serialization;

namespace DiscordBotWebUI.ServerCommunication;

public class ApiResponse
{
    public bool Success { get; }
    public string Message { get; }

    [JsonConstructor]
    public ApiResponse(string message, bool success)
    {
        Message = message;
        Success = success;
    }
}
