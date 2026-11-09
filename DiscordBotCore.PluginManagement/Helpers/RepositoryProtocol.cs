using System.Reflection;

namespace DiscordBotCore.PluginManagement.Helpers;

/// <summary>
/// The contract version this bot speaks with a plugin repository. The other side is ApiProtocol in SethRepository.
/// </summary>
public static class RepositoryProtocol
{
    public const string HeaderName = "X-Seth-Protocol";

    /// <summary>
    /// 2 = NuGet dependencies and the dependencyName/dependencyLink/dependencyLocation fields.
    /// 3 = the repository's API answers only the bot: every request, file downloads included, sends <see cref="HeaderName"/>
    /// and <see cref="UserAgent"/>.
    /// </summary>
    public const int ClientVersion = 3;

    public const int MinimumServerVersion = 3;

    /// <summary>The product name the repository looks for in the <c>User-Agent</c></summary>
    public const string UserAgentProduct = "SethDiscordBot";

    /// <summary><c>SethDiscordBot/&lt;version&gt;</c>, with the version of this assembly</summary>
    public static string UserAgent { get; } = $"{UserAgentProduct}/{Version()}";

    /// <summary>Makes every request of <paramref name="httpClient"/> identify itself as this bot to a plugin repository.</summary>
    public static void Apply(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Remove(HeaderName);
        httpClient.DefaultRequestHeaders.TryAddWithoutValidation(HeaderName, ClientVersion.ToString());
        httpClient.DefaultRequestHeaders.UserAgent.Clear();
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(UserAgent);
    }

    private static string Version()
    {
        Assembly assembly = typeof(RepositoryProtocol).Assembly;
        string? version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                          ?? assembly.GetName().Version?.ToString();

        // Without the build metadata, as in 1.0.0+abc123
        version = version?.Split('+')[0];
        return string.IsNullOrWhiteSpace(version) ? "0" : version;
    }
}
