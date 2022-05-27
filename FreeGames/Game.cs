using PluginManager.Interfaces;
using PluginManager.Items;
using Games.Objects;
using PluginManager.Others;

namespace Games;

public class Game : DBCommand
{
    public string Command => "game";
    public string Description => "Display info about the specified game";
    public string Usage => "game <game>";

    public bool canUseDM => false;
    public bool canUseServer => true;
    public bool requireAdmin => false;

    public async void Execute(Discord.Commands.SocketCommandContext context, Discord.WebSocket.SocketMessage message,
                         Discord.WebSocket.DiscordSocketClient client, bool isDM)
    {

        string game_name = Functions.MergeStrings(message.Content.Split(' '), 1);
        string game_url = await GameData.GetSteamLinkFromGame(game_name);
        if (game_url is null || game_url == null)
        {
            await message.Channel.SendMessageAsync("Could not find the game. Try to be more specific or check for spelling errors.");
            return;
        }
        await context.Channel.SendMessageAsync(game_url);
    }

}
