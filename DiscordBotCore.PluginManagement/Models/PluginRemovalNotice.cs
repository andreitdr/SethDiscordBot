namespace DiscordBotCore.PluginManagement.Models;

/// <summary>How serious the removal of a plugin from the marketplace is, as chosen by whoever removed it</summary>
public enum PluginRemovalSeverity
{
    /// <summary>Obsolete or no longer maintained</summary>
    Notice = 0,

    /// <summary>People should stop using it</summary>
    Warning = 1,

    /// <summary>Unsafe: the bot turns it off until its owner turns it back on</summary>
    Critical = 2
}

/// <summary>
/// What the plugin repository said about an installed plugin that was removed from it. Kept in the local plugin database,
/// so the bot keeps warning about it at every start even when the repository can't be reached.
/// </summary>
public sealed record PluginRemovalNotice(
    DateTimeOffset? RemovedAt, bool RemovedByAdministrator, PluginRemovalSeverity Severity, string? Reason)
{
    /// <summary>From this severity on, the bot turns the plugin off until its owner decides to turn it back on</summary>
    public const PluginRemovalSeverity DisablesAt = PluginRemovalSeverity.Critical;

    public bool DisablesPlugin => Severity >= DisablesAt;

    /// <summary>One sentence for the log and the dashboard</summary>
    public string Describe(string pluginName)
    {
        string by = RemovedByAdministrator ? "the marketplace's administrator" : "its developer";
        string when = RemovedAt is { } at ? $" on {at:yyyy-MM-dd}" : "";
        string why = string.IsNullOrWhiteSpace(Reason) ? "" : $" Reason: \"{Reason}\".";
        string consequence = Severity switch
        {
            PluginRemovalSeverity.Critical => " It was marked unsafe, so the bot turns it off until you turn it back on.",
            PluginRemovalSeverity.Warning => " You should stop using it.",
            _ => ""
        };

        return $"{pluginName} was removed from the marketplace by {by}{when} ({Severity}).{why}{consequence} It can't be reinstalled or updated.";
    }
}

/// <summary>The repository's answer about one installed plugin (route <c>plugin/status</c>, protocol 4)</summary>
public enum OnlinePluginAvailability
{
    Available,
    Removed,

    /// <summary>Never published there, for example a plugin added by hand. Says nothing either way.</summary>
    Unknown
}

public sealed record OnlinePluginStatus(string Name, OnlinePluginAvailability Status, PluginRemovalNotice? Removal);
