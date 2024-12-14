using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;

namespace DiscordBotCore.API.Endpoints.PluginManagement;

public class PluginListEndpoint : IEndpoint
{
    public string Path => "/api/plugin/list/online";
    public EndpointType HttpMethod => EndpointType.Get;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        var onlineInfos = await Application.CurrentApplication.PluginManager.GetPluginsList();

        var response    = await JsonManager.ConvertToJson(onlineInfos, [nameof(PluginOnlineInfo.Name), nameof(PluginOnlineInfo.Author), nameof(PluginOnlineInfo.Version)]);

        return ApiResponse.From(response, true);
    }
}
