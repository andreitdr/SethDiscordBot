
using System.Text.Json.Serialization;

namespace DiscordBotCore.Plugin;

public class OnlineDependencyInfo
{
    public string DependencyName { get; private set; }
    [JsonPropertyName("dependencyLink")]
    public string DownloadLink { get; private set; }
    [JsonPropertyName("dependencyLocation")]
    public string DownloadLocation { get; private set; }
    public bool IsExecutable { get; private set;  }

    [JsonConstructor]
    public OnlineDependencyInfo(string dependencyName, string downloadLink, string downloadLocation, bool isExecutable)
    {
        DependencyName   = dependencyName;
        DownloadLink     = downloadLink;
        DownloadLocation = downloadLocation;
        IsExecutable     = isExecutable;
    }
}
