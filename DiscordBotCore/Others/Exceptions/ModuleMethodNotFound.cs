using System;
using DiscordBotCore.Interfaces.Modules;

namespace DiscordBotCore.Others.Exceptions;

public class ModuleMethodNotFound : Exception
{
    private IModule _SearchedModule;
    public ModuleMethodNotFound(IModule module, string methodName) : base($"Method not found {methodName} in module {module.Name}")
    {
        _SearchedModule = module;
    }
}
