using System.Collections.Generic;
using DiscordBotCore.Interfaces.Modules;
using DiscordBotCore.Modules;

namespace DiscordBotCore.Others.Exceptions;

public class ModuleRequirement
{
    private List<ModuleType> RequiredModulesWithType { get; }
    private List<string> RequiredModulesWithName { get; }
    
    public ModuleRequirement()
    {
        RequiredModulesWithType = new List<ModuleType>();
        RequiredModulesWithName = new List<string>();
    }
    
    public void AddType (ModuleType moduleType)
    {
        RequiredModulesWithType.Add(moduleType);
    }
    
    public void AddName (string moduleName)
    {
        RequiredModulesWithName.Add(moduleName);
    }
    
    public bool RequireAny => RequiredModulesWithType.Count > 0 || RequiredModulesWithName.Count > 0;
    public IList<ModuleType> RequiredModulesWithTypes => RequiredModulesWithType ;
    public IList<string> RequiredModulesWithNames => RequiredModulesWithName;
}
