using PluginManager.Interfaces;
using Games.Objects;
namespace Games;

public class Free : DBCommand
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
        string game_name = PluginManager.Others.Functions.MergeStrings(message.Content.Split(' '), 1);
        string game_url = await GameData.GetSteamLinkFromGame(game_name);
        await context.Channel.SendMessageAsync(game_url);
    }

}
