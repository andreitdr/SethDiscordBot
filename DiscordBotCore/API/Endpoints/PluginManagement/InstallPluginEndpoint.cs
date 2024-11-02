using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.API;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace DiscordBotCore.API.Endpoints.PluginManagement;

public class InstallPluginEndpoint : IEndpoint
{
    public string Path => "/api/plugin/install";
    public EndpointType HttpMethod => EndpointType.Put;
    public async Task<ApiResponse> HandleRequest(string? jsonRequest)
    {
        Dictionary<string, string> jsonDict = await JsonManager.ConvertFromJson<Dictionary<string, string>>(jsonRequest);
        string pluginName = jsonDict["pluginName"];
        
        PluginOnlineInfo? pluginInfo = await Application.CurrentApplication.PluginManager.GetPluginDataByName(pluginName);
        
        if (pluginInfo == null)
        {
            return ApiResponse.Fail("Plugin not found.");
        }

        await Application.CurrentApplication.PluginManager.InstallPlugin(pluginInfo, null);
        return ApiResponse.Ok();
    }
}
