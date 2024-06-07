
using System.Text.Json.Serialization;

namespace DiscordBotCore.Plugin;

public class OnlineDependencyInfo
{
    public string DependencyName { get; private set; }
    public string DownloadLink { get; private set; }
    public string DownloadLocation { get; private set; }

    [JsonConstructor]
    public OnlineDependencyInfo(string dependencyName, string downloadLink, string downloadLocation)
    {
        DependencyName   = dependencyName;
        DownloadLink     = downloadLink;
        DownloadLocation = downloadLocation;
    }
}
