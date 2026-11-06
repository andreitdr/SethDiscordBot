using System.Text.Json.Serialization;

namespace DiscordBotCore.PluginManagement.Models;

/// <summary>A package from nuget.org that a marketplace plugin needs.</summary>
public class OnlineNuGetDependency
{
    public string PackageId { get; private set; }
    public string Version { get; private set; }

    [JsonConstructor]
    public OnlineNuGetDependency(string packageId, string version)
    {
        PackageId = packageId;
        Version = version;
    }
}
