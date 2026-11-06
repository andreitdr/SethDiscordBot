namespace DiscordBotCore.PluginManagement.Helpers;

/// <summary>
/// The contract version this bot speaks with a plugin repository. The other side is ApiProtocol in SethRepository.
/// </summary>
public static class RepositoryProtocol
{
    public const string HeaderName = "X-Seth-Protocol";

    /// <summary>2 = NuGet dependencies and the dependencyName/dependencyLink/dependencyLocation fields</summary>
    public const int ClientVersion = 2;

    public const int MinimumServerVersion = 2;
}
