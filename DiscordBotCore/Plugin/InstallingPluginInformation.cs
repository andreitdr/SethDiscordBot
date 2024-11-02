using System;

namespace DiscordBotCore.Plugin;

public class InstallingPluginInformation
{
    public bool IsInstalling { get; set; }
    public required string PluginName { get; set; }
    public float InstallationProgress { get; set; } = 0.0f;
}
