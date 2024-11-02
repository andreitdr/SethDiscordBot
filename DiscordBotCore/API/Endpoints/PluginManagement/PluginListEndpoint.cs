using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;

namespace DiscordBotCore.API.Endpoints.PluginManagement;

public class PluginListEndpoint : IEndpoint
{
    public string Path => "/api/plugin/list/online";
    public EndpointType HttpMethod => EndpointType.Get;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        var onlineInfos = await Application.CurrentApplication.PluginManager.GetPluginsList();
        var response    = await JsonManager.ConvertToJsonString(onlineInfos);
        return ApiResponse.From(response, true);
    }
}
