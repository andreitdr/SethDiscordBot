using System.Text.Json.Serialization;

namespace DiscordBotWebUI.Models;

public class PluginModel
{
    [JsonPropertyName("PluginName")]
    public string Name { get; set; }
    
    [JsonPropertyName("PluginAuthor")]
    public string Author { get; set; }
    
    [JsonPropertyName("PluginDescription")]
    public string Description { get; set; }

    [JsonConstructor]
    public PluginModel(string name, string author, string description)
    {
        Name        = name;
        Author      = author;
        Description = description;
    }
}
