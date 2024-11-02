using System.Threading.Tasks;
using DiscordBotCore.Others;

namespace DiscordBotCore.API;

public class ApiResponse
{
    public string Message { get; set; }
    public bool Success { get; set; }
    
    private ApiResponse(string message, bool success)
    {
        Message = message;
        Success = success;
    }

    public static ApiResponse From(string message, bool success)
    {
        return new ApiResponse(message, success);
    }

    public static ApiResponse Fail(string message)
    {
        return new ApiResponse(message, false);
    }
    
    public static ApiResponse Ok()
    {
        return new ApiResponse(string.Empty, true);
    }
    
    public async Task<string> ToJson() => await JsonManager.ConvertToJsonString(this);
    
}
