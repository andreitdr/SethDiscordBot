namespace DiscordBotWebUI.Types;

public class MarketItem
{
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    
    public MarketItem(string name, string author, string description)
    {
        this.Name        = name;
        this.Author      = author;
        this.Description = description;
    }
}
