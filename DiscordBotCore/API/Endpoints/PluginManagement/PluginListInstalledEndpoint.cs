using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;

namespace DiscordBotCore.API.Endpoints.PluginManagement;

public class PluginListInstalledEndpoint : IEndpoint
{
    public string Path => "/api/plugin/list/local";
    public EndpointType HttpMethod => EndpointType.Get;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        var listInstalled = await Application.CurrentApplication.PluginManager.GetInstalledPlugins();
        var response      = await JsonManager.ConvertToJsonString(listInstalled);
        return ApiResponse.From(response, true);
    }
}
