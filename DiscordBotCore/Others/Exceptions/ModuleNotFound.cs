using System;
using DiscordBotCore.Interfaces.Modules;

namespace DiscordBotCore.Others.Exceptions;

public class ModuleNotFound : Exception
{
    public ModuleNotFound(string moduleName) : base($"Module not found: {moduleName}")
    {
    }

    public ModuleNotFound(ModuleType moduleType) : base($"No module with type {moduleType} found")
    {
    }
}
