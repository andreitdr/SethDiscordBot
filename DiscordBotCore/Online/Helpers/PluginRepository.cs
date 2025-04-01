using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.PluginManagement;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;
using Microsoft.AspNetCore.Http.Extensions;

namespace DiscordBotCore.Online.Helpers;

public class PluginRepository : IPluginRepository
{
    private readonly IPluginRepositoryConfiguration _pluginRepositoryConfiguration;
    private readonly HttpClient _httpClient;

    public PluginRepository(IPluginRepositoryConfiguration pluginRepositoryConfiguration)
    {
        _pluginRepositoryConfiguration = pluginRepositoryConfiguration;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_pluginRepositoryConfiguration.BaseUrl);
    }
    
    public async Task<List<OnlinePlugin>> GetAllPlugins()
    {
        int operatingSystem = OS.GetOperatingSystemInt();
        bool includeNotApproved = false;

        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-all-plugins", new Dictionary<string, string>
            {
                { "operatingSystem", operatingSystem.ToString() },
                { "includeNotApproved", includeNotApproved.ToString() }
            });
        
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Application.Log("Failed to get all plugins from the repository", LogType.Warning);
            return [];
        }
        
        string content = await response.Content.ReadAsStringAsync();
        List<OnlinePlugin> plugins = await JsonManager.ConvertFromJson<List<OnlinePlugin>>(content);
        
        return plugins;
    }

    public async Task<OnlinePlugin?> GetPluginById(int pluginId)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-plugin", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() },
                { "includeNotApproved", "false" }
            });
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Application.Log("Failed to get plugin from the repository", LogType.Warning);
            return null;
        }
        
        string content = await response.Content.ReadAsStringAsync();
        OnlinePlugin plugin = await JsonManager.ConvertFromJson<OnlinePlugin>(content);
        
        return plugin;
    }

    public async Task<OnlinePlugin?> GetPluginByName(string pluginName)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-plugin-by-name", new Dictionary<string, string>
            {
                { "pluginName", pluginName },
                { "operatingSystem", OS.GetOperatingSystemInt().ToString() },
                { "includeNotApproved", "false" }
            });
        HttpResponseMessage response = await _httpClient.GetAsync(url);

        if (!response.IsSuccessStatusCode)
        {
            Application.Log("Failed to get plugin from the repository", LogType.Warning);
            return null;
        }
        
        string content = await response.Content.ReadAsStringAsync();
        OnlinePlugin plugin = await JsonManager.ConvertFromJson<OnlinePlugin>(content);
        
        return plugin;
    }

    public async Task<List<OnlineDependencyInfo>> GetDependenciesForPlugin(int pluginId)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.DependenciesRepositoryLocation,
            "get-dependencies-for-plugin", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() }
            });
        
        HttpResponseMessage response = await _httpClient.GetAsync(url);
        if(!response.IsSuccessStatusCode)
        {
            Application.Log("Failed to get dependencies for plugin from the repository", LogType.Warning);
            return [];
        }
        
        string content = await response.Content.ReadAsStringAsync();
        List<OnlineDependencyInfo> dependencies = await JsonManager.ConvertFromJson<List<OnlineDependencyInfo>>(content);
        
        return dependencies;
    }

    private string CreateUrlWithQueryParams(string baseUrl, string endpoint, Dictionary<string, string> queryParams)
    {
        QueryBuilder queryBuilder = new QueryBuilder();
        foreach (var(key,value) in queryParams)
        {
            queryBuilder.Add(key, value);
        }
        
        string queryString = queryBuilder.ToQueryString().ToString();
        string url = baseUrl + endpoint + queryString;
        
        return url;
    }
}