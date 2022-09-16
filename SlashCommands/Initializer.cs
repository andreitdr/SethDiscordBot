using Discord.WebSocket;

using PluginManager;
using PluginManager.Interfaces;

using SlashCommands.Items;

namespace SlashCommands
{
    public class Initializer : DBEvent
    {
        public string name => "Slash command engine";

        public string description => "The slash commands initializer and engine";

        public async void Start(DiscordSocketClient client)
        {
            if(!Config.ContainsKey("ServerID") || Config.GetValue<string>("ServerID") == "null" || Config.GetValue<string>("ServerID").Length != 18)
            {
                Console.WriteLine("Invalid Server ID. Change config.json from file and restart bot");
                await Task.Delay(2000);
                return;
            }

            SlashCommandLoader loader = new SlashCommandLoader("./Data/Plugins/SlashCommands/", "dll", client);
            loader.FileLoaded += (args) => Console.WriteLine(args[0] + " => " + args[1]);
            loader.PluginLoaded += (args) => Console.WriteLine(args[0] + " => " + args[1]);
            Globals.commands = await loader.Load();

            client.SlashCommandExecuted += async (args) =>
            {
                foreach (var cmd in Globals.commands)
                {
                    if (cmd.Command == args.Data.Name)
                    {
                        await cmd.ExecuteServer(args);
                        return;
                    }
                }
            };
        }
    }
}