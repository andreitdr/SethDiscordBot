using System.Threading.Tasks;

using Discord.WebSocket;

using PluginManager.Others;
using PluginManager.Interfaces;
using PluginManager.LanguageSystem;
public class LevelingSystem : DBEvent
{
    public string name => "Leveling System";

    public string description => "Leveling System Event";

    public void Start(DiscordSocketClient client)
    {
        client.MessageReceived += Client_MessageReceived;
    }

    private async Task Client_MessageReceived(SocketMessage arg)
    {
        if (arg.Author.IsBot || arg.Attachments.Count > 0 || arg.Content.StartsWith(Functions.readCodeFromFile(System.IO.Path.Combine(Functions.dataFolder, "DiscordBotCore.data"), "BOT_PREFIX", '\t')))
            return;

        if (Core.playerMessages.ContainsKey(arg.Author.Id))
            return;

        (bool x, int lv) = Core.MessageSent(arg.Author.Id, arg.Content.Length);
        Core.playerMessages.Add(arg.Author.Id, arg.Content);
        if (x)
            if (Language.ActiveLanguage != null)
                await arg.Channel.SendMessageAsync(Language.ActiveLanguage.LanguageWords["DB_EVENT_LEVEL_SYSTEM_LEVEL_UP"].Replace("{0}", lv.ToString()));
            else await arg.Channel.SendMessageAsync("You've successfully leveled up to level " + lv);

    }
}

