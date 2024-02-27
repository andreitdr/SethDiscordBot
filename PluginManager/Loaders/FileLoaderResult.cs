namespace PluginManager.Loaders;

public class FileLoaderResult
{
    public string PluginName { get; init; }

    public string? ErrorMessage { get; init; }


    public FileLoaderResult(string pluginName, string errorMessage)
    {
        PluginName   = pluginName;
        ErrorMessage = errorMessage;
    }

    public FileLoaderResult(string pluginName)
    {
        PluginName = pluginName;
    }
}
