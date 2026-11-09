using System.Net;
using System.Text.Json;
using DiscordBotCore.Logging;
using DiscordBotCore.PluginManagement.Models;
using DiscordBotCore.Utilities;

namespace DiscordBotCore.PluginManagement.Helpers;

public class PluginRepository : IPluginRepository
{
    private readonly IPluginRepositoryConfiguration _pluginRepositoryConfiguration;
    private readonly HttpClient _httpClient;
    private readonly ILogger _logger;
    private RepositoryCompatibility? _knownCompatibility;
    private string? _knownCompatibilityBaseUrl;
    private int _knownServerVersion;

    public PluginRepository(IPluginRepositoryConfiguration pluginRepositoryConfiguration, ILogger logger)
        : this(pluginRepositoryConfiguration, logger, new HttpClient())
    {
    }

    public PluginRepository(IPluginRepositoryConfiguration pluginRepositoryConfiguration, ILogger logger, HttpClient httpClient)
    {
        _pluginRepositoryConfiguration = pluginRepositoryConfiguration;
        _httpClient = httpClient;
        RepositoryProtocol.Apply(_httpClient);
        _logger = logger;
    }

    public bool IsConfigured => _pluginRepositoryConfiguration.IsConfigured;
    
    public async Task<List<OnlinePlugin>> GetAllPlugins(int operatingSystem, bool includeNotApproved)
    {
        if (!await CanRequestAsync())
        {
            return [];
        }

        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-all-plugins", new Dictionary<string, string>
            {
                { "operatingSystem", operatingSystem.ToString() },
                { "includeNotApproved", includeNotApproved.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_pluginRepositoryConfiguration.BaseUrl + url);

            if (!response.IsSuccessStatusCode)
            {
                return [];
            }

            string content = await response.Content.ReadAsStringAsync();
            List<OnlinePlugin> plugins = await JsonManager.ConvertFromJson<List<OnlinePlugin>>(content);

            return plugins;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            _logger.LogException(exception,this);
            return [];
        }

    }

    public async Task<OnlinePlugin?> GetPluginById(int pluginId)
    {
        if (!await CanRequestAsync())
        {
            return null;
        }

        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-by-id", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() },
                { "includeNotApproved", "false" }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_pluginRepositoryConfiguration.BaseUrl + url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            string content = await response.Content.ReadAsStringAsync();
            OnlinePlugin plugin = await JsonManager.ConvertFromJson<OnlinePlugin>(content);

            return plugin;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            _logger.LogException(exception, this);
            return null;
        }

    }

    public async Task<OnlinePlugin?> GetPluginByName(string pluginName, int operatingSystem, bool includeNotApproved)
    {
        if (!await CanRequestAsync())
        {
            return null;
        }

        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.PluginRepositoryLocation,
            "get-by-name", new Dictionary<string, string>
            {
                { "pluginName", pluginName },
                { "operatingSystem", operatingSystem.ToString() },
                { "includeNotApproved", includeNotApproved.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_pluginRepositoryConfiguration.BaseUrl + url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.Log($"Plugin {pluginName} not found");
                return null;
            }

            string content = await response.Content.ReadAsStringAsync();
            OnlinePlugin plugin = await JsonManager.ConvertFromJson<OnlinePlugin>(content);

            return plugin;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            _logger.LogException(exception, this);
            return null;
        }

    }

    public async Task<List<OnlineDependencyInfo>> GetDependenciesForPlugin(int pluginId)
    {
        if (!await CanRequestAsync())
        {
            return [];
        }

        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.DependenciesRepositoryLocation,
            "get-by-plugin-id", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_pluginRepositoryConfiguration.BaseUrl + url);
            if(!response.IsSuccessStatusCode)
            {
                _logger.Log($"Failed to get dependencies for plugin with ID {pluginId}");
                return [];
            }
        
            string content = await response.Content.ReadAsStringAsync();
            List<OnlineDependencyInfo> dependencies = await JsonManager.ConvertFromJson<List<OnlineDependencyInfo>>(content);
        
            return dependencies;
        }
        catch (Exception exception) when (exception is HttpRequestException or System.Text.Json.JsonException)
        {
            _logger.LogException(exception, this);
            return [];
        }

    }

    public async Task<List<OnlineNuGetDependency>?> GetNuGetDependenciesForPlugin(int pluginId)
    {
        if (!await CanRequestAsync())
        {
            return null;
        }

        string url = CreateUrlWithQueryParams(_pluginRepositoryConfiguration.DependenciesRepositoryLocation,
            "get-nuget-by-plugin-id", new Dictionary<string, string>
            {
                { "pluginId", pluginId.ToString() }
            });

        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(_pluginRepositoryConfiguration.BaseUrl + url);
            if (!response.IsSuccessStatusCode)
            {
                _logger.Log($"Failed to get NuGet dependencies for plugin with ID {pluginId}");
                return null;
            }

            string content = await response.Content.ReadAsStringAsync();
            return await JsonManager.ConvertFromJson<List<OnlineNuGetDependency>>(content);
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException)
        {
            _logger.LogException(exception, this);
            return null;
        }
    }

    public async Task<RepositoryCompatibility> CheckCompatibilityAsync(bool recheck = false)
    {
        if (!IsConfigured)
        {
            return RepositoryCompatibility.Unreachable;
        }

        string baseUrl = _pluginRepositoryConfiguration.BaseUrl;
        if (!recheck && _knownCompatibility is { } known && _knownCompatibilityBaseUrl == baseUrl)
        {
            return known;
        }

        RepositoryCompatibility compatibility = await AskRepositoryAsync(baseUrl);
        _knownCompatibility = compatibility == RepositoryCompatibility.Unreachable ? null : compatibility;
        _knownCompatibilityBaseUrl = baseUrl;

        if (compatibility != RepositoryCompatibility.Compatible)
        {
            _logger.Log($"{baseUrl}: {compatibility.Describe()}", this, LogType.Warning);
        }

        return compatibility;
    }

    private async Task<RepositoryCompatibility> AskRepositoryAsync(string baseUrl)
    {
        try
        {
            using HttpResponseMessage response = await _httpClient.GetAsync(baseUrl + "compatibility");

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return RepositoryCompatibility.ServerTooOld;
            }

            if (!response.IsSuccessStatusCode)
            {
                return RepositoryCompatibility.Unreachable;
            }

            using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            int serverVersion = document.RootElement.GetProperty("protocolVersion").GetInt32();
            int minimumClientVersion = document.RootElement.GetProperty("minimumClientProtocolVersion").GetInt32();
            _knownServerVersion = serverVersion;

            if (RepositoryProtocol.ClientVersion < minimumClientVersion)
            {
                return RepositoryCompatibility.BotTooOld;
            }

            return serverVersion < RepositoryProtocol.MinimumServerVersion
                ? RepositoryCompatibility.ServerTooOld
                : RepositoryCompatibility.Compatible;
        }
        catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException)
        {
            _logger.LogException(exception, this);
            return RepositoryCompatibility.Unreachable;
        }
        catch (Exception exception) when (exception is JsonException or KeyNotFoundException or InvalidOperationException or FormatException)
        {
            // Something answered, but it doesn't speak this protocol
            _logger.LogException(exception, this);
            return RepositoryCompatibility.ServerTooOld;
        }
    }

    public async Task<List<OnlinePluginStatus>?> GetPluginStatuses(IReadOnlyCollection<string> pluginNames)
    {
        if (!await CanRequestAsync() || _knownServerVersion < RepositoryProtocol.StatusServerVersion)
        {
            return null;
        }

        var statuses = new List<OnlinePluginStatus>();
        foreach (string[] chunk in pluginNames.Distinct(StringComparer.OrdinalIgnoreCase).Chunk(RepositoryProtocol.MaxStatusNamesPerRequest))
        {
            string query = string.Join("&", chunk.Select(name => $"names={Uri.EscapeDataString(name)}"));
            string url = _pluginRepositoryConfiguration.BaseUrl + _pluginRepositoryConfiguration.PluginRepositoryLocation + "status?" + query;

            try
            {
                using HttpResponseMessage response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.Log($"The plugin repository couldn't say whether installed plugins were removed ({(int)response.StatusCode}).", this, LogType.Warning);
                    return null;
                }

                statuses.AddRange(ParseStatuses(await response.Content.ReadAsStringAsync()));
            }
            catch (Exception exception) when (exception is HttpRequestException or TaskCanceledException or JsonException
                                                  or KeyNotFoundException or InvalidOperationException or FormatException)
            {
                _logger.LogException(exception, this);
                return null;
            }
        }

        return statuses;
    }

    /// <summary>
    /// Reads <c>plugin/status</c>: a list of <c>{Name, Status, RemovedAt, RemovedBy, Severity, Reason}</c>, with the
    /// enums as names. Anything this bot doesn't recognise is treated as Unknown, so it never acts on a guess.
    /// </summary>
    internal static List<OnlinePluginStatus> ParseStatuses(string json)
    {
        using JsonDocument document = JsonDocument.Parse(json);
        var statuses = new List<OnlinePluginStatus>();

        foreach (JsonElement item in document.RootElement.EnumerateArray())
        {
            string name = GetString(item, "Name") ?? throw new FormatException("A plugin status has no name.");
            string? statusText = GetString(item, "Status");
            OnlinePluginAvailability availability =
                statusText is not null && !char.IsDigit(statusText.FirstOrDefault())
                && Enum.TryParse(statusText, true, out OnlinePluginAvailability parsedStatus) && Enum.IsDefined(parsedStatus)
                    ? parsedStatus
                    : OnlinePluginAvailability.Unknown;

            PluginRemovalNotice? removal = null;
            if (availability == OnlinePluginAvailability.Removed)
            {
                // A severity this bot doesn't know is newer than it, so it's taken as the most serious one it knows
                string? severityText = GetString(item, "Severity");
                PluginRemovalSeverity severity = severityText is not null && !char.IsDigit(severityText.FirstOrDefault())
                    && Enum.TryParse(severityText, true, out PluginRemovalSeverity parsed) && Enum.IsDefined(parsed)
                    ? parsed
                    : PluginRemovalSeverity.Critical;
                DateTimeOffset? removedAt = DateTimeOffset.TryParse(GetString(item, "RemovedAt"), out DateTimeOffset at) ? at : null;
                bool byAdministrator = string.Equals(GetString(item, "RemovedBy"), "Administrator", StringComparison.OrdinalIgnoreCase);
                removal = new PluginRemovalNotice(removedAt, byAdministrator, severity, GetString(item, "Reason"));
            }

            statuses.Add(new OnlinePluginStatus(name, availability, removal));
        }

        return statuses;
    }

    private static string? GetString(JsonElement item, string property)
    {
        foreach (JsonProperty candidate in item.EnumerateObject())
        {
            if (string.Equals(candidate.Name, property, StringComparison.OrdinalIgnoreCase))
            {
                return candidate.Value.ValueKind == JsonValueKind.String ? candidate.Value.GetString() : null;
            }
        }

        return null;
    }

    private async Task<bool> CanRequestAsync() =>
        IsConfigured && await CheckCompatibilityAsync() == RepositoryCompatibility.Compatible;

    internal static string CreateUrlWithQueryParams(string baseUrl, string endpoint, Dictionary<string, string> queryParams)
    {
        string queryString = string.Join("&", queryParams.Select(pair =>
            $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));

        string url = baseUrl + endpoint;
        if (queryString.Length > 0)
        {
            url += "?" + queryString;
        }
        
        return url;
    }
}