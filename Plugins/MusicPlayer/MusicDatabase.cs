using DiscordBotCore.Others;

namespace MusicPlayer;

public class MusicDatabase: SettingsDictionary<string, MusicInfo>
{
    public MusicDatabase(string file): base(file)
    {
    }

    /// <summary>
    /// Checks if the database contains a melody with the specified name or alias
    /// </summary>
    /// <param name="melodyName">The name (alias) of the melody</param>
    /// <returns></returns>
    public bool ContainsMelodyWithNameOrAlias(string melodyName)
    {
        return ContainsKey(melodyName) || Values.Any(elem => elem.Aliases.Contains(melodyName, StringComparer.CurrentCultureIgnoreCase));
    }

    /// <summary>
    /// Tries to get the music info of a melody with the specified name or alias. Returns the first match or null if no match was found.
    /// </summary>
    /// <param name="searchQuery">The music name or one of its aliases.</param>
    /// <returns></returns>
    public MusicInfo? GetMusicInfo(string searchQuery)
    {
        return FirstOrDefault(kvp => kvp.Key.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                                          kvp.Value.Aliases.Any(alias => alias.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase))
        ).Value;
    }

    /// <summary>
    /// Get a list of music info that match the search query. Returns null if an error occurred, or empty list if no match was found.
    /// </summary>
    /// <param name="searchQuery">The search query</param>
    /// <returns>null if an error occured, otherwise a list with songs that match the search query. If no song match the list is empty</returns>
    public List<MusicInfo>? GetMusicInfoList(string searchQuery)
    {
        try
        {
            return this.Where(kvp =>
                           kvp.Key.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase) ||
                           kvp.Value.Aliases.Any(alias => alias.Contains(searchQuery, StringComparison.InvariantCultureIgnoreCase))
                       )
                       .Select(item => item.Value).ToList();
        }

        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
            return null;
        }
    }

    /// <summary>
    /// Adds a new entry to the database based on the music info. It uses the title as the key.
    /// </summary>
    /// <param name="musicInfo">The music to add to</param>
    public void AddNewEntryBasedOnMusicInfo(MusicInfo musicInfo)
    {
        Add(musicInfo.Title, musicInfo);
    }
}
