using System.Text.Json.Serialization;

namespace DiscordBotCore.PluginManagement.Models;

public class OnlinePlugin
{
    public int Id { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }
    public string Version { get; private set; }
    public string Author { get; private set; }
    public string DownloadLink { get; private set; }
    public int OperatingSystem { get; private set; }
    public bool IsApproved { get; private set; }

    [JsonConstructor]
    public OnlinePlugin(int id, string name, string description, string version,
        string author, string downloadLink, int operatingSystem, bool isApproved)
    {
        Id = id;
        Name = name;
        Description = description;
        Version = version;
        Author = author;
        DownloadLink = downloadLink;
        OperatingSystem = operatingSystem;
        IsApproved = isApproved;
    }
}