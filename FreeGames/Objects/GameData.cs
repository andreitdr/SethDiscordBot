using PluginManager.Online;
namespace Games.Objects;

public class GameData
{
    internal async static Task<string> GetSteamLinkFromGame(string GameName)
    {
        string URL = $"https://store.steampowered.com/search?term={GameName.Replace(" ", "+")}";
        List<string> lines = await ServerCom.ReadTextFromFile(URL);

        string? gameData = (
                        from s in lines
                        where s.Contains(GameName.Replace(" ", "_"), StringComparison.OrdinalIgnoreCase)
                        select s).FirstOrDefault();
        if (gameData is null) return null;
        string GameURL = gameData.Split('\"')[1].Split('?')[0];

        if (GameURL == "menuitem")
            return null;

        return GameURL;

    }
}
