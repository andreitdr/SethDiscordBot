namespace DiscordBotCore.Resources;

/// <summary>
/// This values are used in the default Config file
/// </summary>
public static class DefaultConfigurationValues
{
    public static readonly string DefaultLogFormat = "{ThrowTime} {SenderName} {Message}";
    public static readonly string DefaultLogFolder = "./Data/Logs";
    public static readonly string DefaultResourcesFolder = "./Data/Resources";
    public static readonly string DefaultConfigFile = "./Data/Resources/config.json";
    public static readonly string DefaultPluginFolder = "./Data/Plugins";
    public static readonly string DefaultPluginDatabaseFile = "./Data/Resources/plugins.db";
    public static readonly string DefaultMaxHistorySize = "1000";
}