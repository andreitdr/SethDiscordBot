namespace DiscordBotCore.PluginManagement.Models;

public class LocalDependencyInfo
{
    public Guid Id { get; internal set; }
    public string DependencyName { get; internal set; }
    public string DependencyLocation { get; internal set; }
    public Guid PluginId { get; internal set; }
    
}