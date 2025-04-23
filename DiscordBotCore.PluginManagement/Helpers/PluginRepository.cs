using System.Net.Mime;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities;
using Microsoft.AspNetCore.Http.Extensions;

namespace DiscordBotCore.PluginManagement.Helpers;

public class PluginRepository : IPluginRepository
{
    private readonly IPluginRepositoryConfiguration _pluginRepositoryConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;

    public PluginRepository(IPluginRepositoryConfiguration pluginRepositoryConfiguration, ILogger logger)
    {
        _pluginRepositoryConfiguration = pluginRepositoryConfiguration;
        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(_pluginRepositoryConfiguration.BaseUrl);
        _logger = logger;
    }
    
    public async Task<List<OnlinePlugin>> GetAllPlugins(int operatingSystem, bool includeNotApproved)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-all-plugins", new Dictionary<string, string>
            {
                { "operatingSystem", operatingSystem.ToString() },
                { "includeNotApproved", includeNotApproved.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            string content = await response.Content.ReadAsStringAsync();
            List<OnlinePlugin> plugins = await JsonManager.ConvertFromJson<List<OnlinePlugin>>(content);

            return plugins;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogException(exception,this);
            return [];
        }

    }

    public async Task<OnlinePlugin?> GetPluginById(int pluginId)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-by-id", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() },
                { "includeNotApproved", "false" }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string content = await response.Content.ReadAsStringAsync();
            OnlinePlugin plugin = await JsonManager.ConvertFromJson<OnlinePlugin>(content);

            return plugin;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogException(exception, this);
            return null;
        }

    }

    public async Task<OnlinePlugin?> GetPluginByName(string pluginName, int operatingSystem, bool includeNotApproved)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-by-name", new Dictionary<string, string>
            {
                { "pluginName", pluginName },
                { "operatingSystem", operatingSystem.ToString() },
                { "includeNotApproved", includeNotApproved.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Log($"Plugin {pluginName} not found");
                return null;
            }

            string content = await response.Content.ReadAsStringAsync();
            OnlinePlugin plugin = await JsonManager.ConvertFromJson<OnlinePlugin>(content);

            return plugin;
        }
        catch (HttpRequestException exception)
        {
            _logger.LogException(exception, this);
            return null;
        }

    }

    public async Task<List<OnlineDependencyInfo>> GetDependenciesForPlugin(int pluginId)
    {
        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.DependenciesRepositoryLocation,
            "get-by-plugin-id", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(url);
            if(!response.IsSuccessStatusCode)
            {
                _logger.Log($"Failed to get dependencies for plugin with ID {pluginId}");
                return [];
            }
        
            string content = await response.Content.ReadAsStringAsync();
            List<OnlineDependencyInfo> dependencies = await JsonManager.ConvertFromJson<List<OnlineDependencyInfo>>(content);
        
            return dependencies;
        }catch(HttpRequestException exception)
        {
            _logger.LogException(exception, this);
            return [];
        }

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