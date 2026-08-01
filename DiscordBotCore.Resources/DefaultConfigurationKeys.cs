namespace DiscordBotCore.Resources;

public static class DefaultConfigurationKeys
{

    public static class RemotePluginConnectionConfiguration
    {
        public static readonly string Details = "RemotePluginConnectionConfigurationDetails";

        public static readonly string BaseUrl = "BaseUrl";
        public static readonly string PluginsEndpoint = "PluginsEndpoint";
        public static readonly string DependenciesEndpoint = "DependenciesEndpoint";
    }
    
    
    public static readonly string PluginFolder = "PluginFolder";
    public static readonly string ResourcesFolder = "ResourcesFolder";
    public static readonly string PluginsDatabaseFile = "PluginDatabase";
    
}