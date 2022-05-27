using System.Threading.Tasks;

using Discord.WebSocket;

using PluginManager.Others;
using PluginManager.Interfaces;
using PluginManager.LanguageSystem;
using PluginManager.Items;
using System;

public class LevelingSystem : DBEvent
{
    public string name => "Leveling System";

    public string description => "Leveling System Event";

    public void Start(DiscordSocketClient client)
    {

        ConsoleCommandsHandler.AddCommand("lvl", "calos command extern", async (args) =>
        {
            Console.WriteLine("Leveling system command");

        });

        client.MessageReceived += Client_MessageReceived;
    }

    private async Task Client_MessageReceived(SocketMessage arg)
    {
        if (arg.Author.IsBot || arg.Attachments.Count > 0 ||
            arg.Content.StartsWith
            (
                Functions.readCodeFromFile
                (
                    fileName: System.IO.Path.Combine(Functions.dataFolder, "DiscordBotCore.data"),
                    Code: "BOT_PREFIX",
                    separator: '='
                )
            )
        )
            return;
        //Console_Utilities.WriteColorText("Message from : " + arg.Author.Username);
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

