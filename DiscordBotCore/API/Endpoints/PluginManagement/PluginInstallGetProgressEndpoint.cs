using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;

namespace DiscordBotCore.API.Endpoints.PluginManagement;

public class PluginInstallGetProgressEndpoint : IEndpoint
{
    public string Path => "/api/plugin/install/progress";
    public EndpointType HttpMethod => EndpointType.Get;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        if (!Application.CurrentApplication.PluginManager.InstallingPluginInformation.IsInstalling)
        {
            return ApiResponse.Fail("No plugin is currently being installed.");    
        }
        
        var progress = Application.CurrentApplication.PluginManager.InstallingPluginInformation.InstallationProgress;
        string stringProgress = progress.ToString(CultureInfo.InvariantCulture);
        var response = new Dictionary<string, string>
        {
            {"progress", stringProgress},
            {"pluginName", Application.CurrentApplication.PluginManager.InstallingPluginInformation.PluginName}
        };
        return ApiResponse.From(await JsonManager.ConvertToJsonString(response), true);
    }
}
