using DiscordBotCore.Interfaces.Modules;

namespace DiscordBotCore.Modules;

public class ModuleOnlineData
{
    public string ModuleName { get; set; }
    public string ModuleDownloadUrl { get; set; }
    public string ModuleDescription { get; set; }
    public string ModuleAuthor { get; set; }
    public ModuleType ModuleType { get; set; }

    public ModuleOnlineData(string moduleName, string moduleDownloadUrl, ModuleType moduleType, string moduleDescription, string moduleAuthor)
    {
        ModuleName = moduleName;
        ModuleDownloadUrl = moduleDownloadUrl;
        ModuleType = moduleType;
        ModuleDescription = moduleDescription;
        ModuleAuthor = moduleAuthor;
    }
}
