namespace DiscordBotWebUI.Types;
public enum ItemType
{
    Module,
    Plugin
}


public class MarketItem
{
    public ItemType Type { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public string Description { get; set; }
    
    public MarketItem(string name, string author, string description, ItemType type)
    {
        this.Name        = name;
        this.Author      = author;
        this.Description = description;
        this.Type        = type;
    }
}
