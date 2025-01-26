using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DiscordBotCore.Interfaces.PluginManagement;
using DiscordBotCore.Others;
using DiscordBotCore.Plugin;

namespace DiscordBotCore.Online.Helpers;

internal class PluginRepository : IPluginRepository
{
    private readonly IPluginRepositoryConfiguration _pluginRepositoryConfiguration;
    private readonly HttpClient _httpClient;
    internal PluginRepository(IPluginRepositoryConfiguration pluginRepositoryConfiguration)
    {
        _pluginRepositoryConfiguration = pluginRepositoryConfiguration;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new System.Uri(_pluginRepositoryConfiguration.BaseUrl);
    }


    public async Task<List<OnlinePlugin>> GetAllPlugins()
    {
        HttpResponseMessage response = 
            await _httpClient.GetAsync(_pluginRepositoryConfiguration.PluginRepositoryLocation + "get-all-plugins");

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
        HttpResponseMessage response =
            await _httpClient.GetAsync(_pluginRepositoryConfiguration.PluginRepositoryLocation +
                                       $"get-plugin/{pluginId}");

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
        HttpResponseMessage response =
            await _httpClient.GetAsync(_pluginRepositoryConfiguration.PluginRepositoryLocation +
                                       $"get-plugin-by-name/{pluginName}");

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
        HttpResponseMessage response = await _httpClient.GetAsync(_pluginRepositoryConfiguration.DependenciesRepositoryLocation + $"get-dependencies-for-plugin/{pluginId}");
        if(!response.IsSuccessStatusCode)
        {
            Application.Log("Failed to get dependencies for plugin from the repository", LogType.Warning);
            return [];
        }
        
        string content = await response.Content.ReadAsStringAsync();
        List<OnlineDependencyInfo> dependencies = await JsonManager.ConvertFromJson<List<OnlineDependencyInfo>>(content);
        
        return dependencies;
    }
}