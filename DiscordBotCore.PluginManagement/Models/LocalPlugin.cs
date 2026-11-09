namespace DiscordBotCore.PluginManagement.Models;

public class LocalPlugin
{
    public Guid Id { get; internal set; }
    public string PluginName { get; internal set; }
    public string PluginVersion { get; internal set; }
    public string FilePath { get; internal set; }
    public bool IsOfflineAdded { get; internal set; }
    public bool IsEnabled { get; internal set; }

    /// <summary>Set when the plugin repository said this plugin was removed from it</summary>
    public PluginRemovalNotice? RemovalNotice { get; internal set; }

    /// <summary>
    /// The most serious removal the bot's owner chose to keep running the plugin despite. A notice that's more serious than
    /// this, and serious enough, turns the plugin off again.
    /// </summary>
    public PluginRemovalSeverity? AcknowledgedRemovalSeverity { get; internal set; }

    /// <summary>Turned off because of <see cref="RemovalNotice"/>, and waiting for the owner to decide</summary>
    public bool IsDisabledByRemoval =>
        !IsEnabled && RemovalNotice is { DisablesPlugin: true } notice && !(AcknowledgedRemovalSeverity >= notice.Severity);
}
