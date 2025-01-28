using DiscordBotCore.Online;

namespace DiscordBotCore.API.Endpoints;

public class ApiEndpointBase
{
    internal IPluginManager PluginManager { get; }
    public ApiEndpointBase(IPluginManager pluginManager)
    {
        PluginManager = pluginManager;
    }
}