namespace PluginManager.Loaders;

public class FileLoaderResult
{
    public string PluginName { get; private set; }

    public string ErrorMessage { get; private set; }


    public FileLoaderResult(string pluginName, string errorMessage)
    {
        PluginName   = pluginName;
        ErrorMessage = errorMessage;
    }

    public FileLoaderResult(string pluginName)
    {
        PluginName = pluginName;
        ErrorMessage = string.Empty;
    }
}
