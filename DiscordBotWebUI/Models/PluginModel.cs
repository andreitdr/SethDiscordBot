using System.Text.Json.Serialization;

namespace DiscordBotWebUI.Models;

public class PluginModel
{
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }

    [JsonConstructor]
    public PluginModel(string name, string author, string description)
    {
        Name        = name;
        Author      = author;
        Description = description;
    }
}
