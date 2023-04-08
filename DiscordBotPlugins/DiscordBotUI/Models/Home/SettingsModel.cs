
using Discord;
using Discord.WebSocket;


namespace DiscordBotUI.Models.Home;

public class SettingsModel
{
    public string BotToken { get; set; }
    public string BotPrefix { get; set; }
    public string ServerID { get; set; }
}
