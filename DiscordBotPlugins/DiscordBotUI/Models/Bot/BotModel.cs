using PluginManager.Interfaces;

namespace DiscordBotUI.Models.Bot
{
    public class BotModel
    {
        public string BotName { get; set; }
        public string StartStatus { get; set; }
        public int PluginsLoaded { get; set; }

        public List<DBCommand> Commands { get; set; }
        public List<DBEvent> Events { get; set; }
        public List<DBSlashCommand> SlashCommands { get; set; }
    }
}
