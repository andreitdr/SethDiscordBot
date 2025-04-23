using System.Reflection;
using System.Runtime.Loader;
using DiscordBotCore.Logging;

namespace DiscordBotCore.PluginManagement.Loading;

public class PluginLoaderContext : AssemblyLoadContext
{
    private readonly ILogger _logger;

    public PluginLoaderContext(ILogger logger, string name) : base(name: name, isCollectible: true)
    {
        _logger = logger;
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        _logger.Log("Assembly load requested: " + assemblyName.Name, this);
        return base.Load(assemblyName);
    }
}