using System;

namespace DiscordBotCore.Others.Exceptions;

public class DependencyNotFoundException : Exception
{
    private string PluginName { get; set; }
    public DependencyNotFoundException(string message): base(message)
    {
        
    }
    
    public DependencyNotFoundException(string message, string pluginName): base(message)
    {
        this.PluginName = pluginName;
    }
    
}
