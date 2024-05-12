using DiscordBotCore.Others;

namespace DiscordBotCore.Loaders;

public class PluginLoadResultData
{
    public string PluginName { get; init; }
    public PluginType PluginType { get; init; }
    public string? ErrorMessage { get; init; }
    public bool IsSuccess { get; init; }

    public object Plugin { get; init; }

    public PluginLoadResultData(string pluginName, PluginType pluginType, bool isSuccess, string? errorMessage = null,
        object? plugin = null)
    {
        PluginName   = pluginName;
        PluginType   = pluginType;
        IsSuccess    = isSuccess;
        ErrorMessage = errorMessage;
        Plugin       = plugin is null ? new() : plugin;
    }
}
