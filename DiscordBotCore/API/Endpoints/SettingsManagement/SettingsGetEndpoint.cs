using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;

namespace DiscordBotCore.API.Endpoints.SettingsManagement;

public class SettingsGetEndpoint : IEndpoint
{
    public string Path => "/api/settings/get";
    public EndpointType HttpMethod => EndpointType.Get;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        if (string.IsNullOrEmpty(jsonRequest))
        {
            return ApiResponse.Fail("The json from the request was empty");
        }

        Dictionary<string, object> jsonSettingsDictionary = new Dictionary<string, object>()
        {
            {"token", Application.CurrentApplication.ApplicationEnvironmentVariables.Get("token", string.Empty)},
            {"prefix", Application.CurrentApplication.ApplicationEnvironmentVariables.Get("prefix", string.Empty)},
            {"ServerIDs", Application.CurrentApplication.ApplicationEnvironmentVariables.GetList("ServerIDs", new List<ulong>())}
        };

        string jsonResponse = await JsonManager.ConvertToJsonString(jsonSettingsDictionary);
        return ApiResponse.From(jsonResponse, true);
    }
}
