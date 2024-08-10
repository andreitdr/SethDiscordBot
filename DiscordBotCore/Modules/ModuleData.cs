using System.Collections.Generic;

namespace DiscordBotCore.Modules;

public class ModuleData
{
    public string ModuleName { get; set; }
    public string ModulePath { get; set; }
    public bool IsEnabled { get; set; } = true;
    
    public ModuleData(string moduleName, string modulePath, bool isEnabled)
    {
        ModuleName = moduleName;
        ModulePath = modulePath;
        IsEnabled = isEnabled;
    }
}
