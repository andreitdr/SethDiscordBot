using System.Reflection;
using System.Runtime.Loader;

namespace DiscordBotCore.PluginManagement.Loading;

public class PluginLoaderContext : AssemblyLoadContext
{
    public PluginLoaderContext(string name) : base(name: name, isCollectible: true) {}

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        
        return base.Load(assemblyName);
    }
}