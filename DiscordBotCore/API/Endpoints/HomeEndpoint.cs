using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;
using Microsoft.AspNetCore.Http;

namespace DiscordBotCore.API.Endpoints;

internal class HomeEndpoint : IEndpoint
{
    private static readonly string _HomeMessage = "Welcome to the DiscordBot API.";
    public string Path => "/";
    EndpointType IEndpoint.HttpMethod => EndpointType.Get;
    public async Task<ApiResponse> HandleRequest(string? jsonText)
    {
        string response = _HomeMessage;
        if (jsonText != string.Empty)
        {
            var json = await JsonManager.ConvertFromJson<Dictionary<string,string>>(jsonText!);
            response += $"\n\nYou sent the following JSON:\n{string.Join("\n", json.Select(x => $"{x.Key}: {x.Value}"))}";
        }
        
        return ApiResponse.From(response, true);
    }
}
