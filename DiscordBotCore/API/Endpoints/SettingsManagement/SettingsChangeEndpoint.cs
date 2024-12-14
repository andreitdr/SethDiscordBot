using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;

namespace DiscordBotCore.API.Endpoints.SettingsManagement;

public class SettingsChangeEndpoint : IEndpoint
{
    public string Path => "/api/settings/update";
    public EndpointType HttpMethod => EndpointType.Post;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        if (string.IsNullOrEmpty(jsonRequest))
        {
            return ApiResponse.Fail("Invalid json string");
        }

        Dictionary<string, string> jsonDictionary = await JsonManager.ConvertFromJson<Dictionary<string, string>>(jsonRequest);
        foreach (var (key, value) in jsonDictionary)
        {
            Application.CurrentApplication.ApplicationEnvironmentVariables.Set(key, value);
        }

        return ApiResponse.Ok();
    }
}
