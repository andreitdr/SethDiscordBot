using System.Collections.Generic;

namespace DiscordBotCore.Modules;

public class ModuleData
{
    public string ModuleName { get; set; }
    public string ModulePath { get; set; }
    public bool IsEnabled { get; set; } = true;
    public IDictionary<string, string> MethodMapping { get; set; }
    
    public ModuleData(string moduleName, string modulePath, IDictionary<string, string> methodMapping, bool isEnabled)
    {
        ModuleName = moduleName;
        ModulePath = modulePath;
        MethodMapping = methodMapping;
        IsEnabled = isEnabled;
    }
}
