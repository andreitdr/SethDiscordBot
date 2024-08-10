using System.Collections.Generic;
using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Modules;

namespace DiscordBotCore.Others.Exceptions;

public class RequireInstallModule
{
    private List<ModuleType> RequiredModulesWithType { get; }
    
    public RequireInstallModule()
    {
        RequiredModulesWithType = new List<ModuleType>();
    }
    
    public void AddType (ModuleType moduleType)
    {
        RequiredModulesWithType.Add(moduleType);
    }
    
    public bool RequireAny => RequiredModulesWithType.Count > 0;
    public IList<ModuleType> RequiredModules => RequiredModulesWithType;
}
