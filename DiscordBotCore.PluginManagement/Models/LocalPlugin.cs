namespace DiscordBotCore.PluginManagement.Models;

public class LocalPlugin
{
    public Guid Id { get; internal set; }
    public string PluginName { get; internal set; }
    public string PluginVersion { get; internal set; }
    public string FilePath { get; internal set; }
    public bool IsOfflineAdded { get; internal set; }
    public bool IsEnabled { get; internal set; }
}
