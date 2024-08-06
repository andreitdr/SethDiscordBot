using Discord;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace MusicPlayer.Commands;

public class SearchMelody: DBCommand
{

    public string Command => "search_melody";
    public List<string>? Aliases => null;
    public string Description => "Search for a melody in the database";
    public string Usage => "search_melody [melody name OR one of its aliases]";
    public bool requireAdmin => false;

    public void ExecuteServer(DbCommandExecutingArguments args)
    {
        
        if(Variables._MusicDatabase is null)
        {
            args.Context.Channel.SendMessageAsync("Music Database is not loaded !");
            return;
        }
        
        if (args.Arguments is null || args.Arguments.Length == 0)
        {
            args.Context.Channel.SendMessageAsync("You need to specify a melody name");
            return;
        }
        
        var title = string.Join(" ", args.Arguments);

        if (string.IsNullOrWhiteSpace(title))
        {
            args.Context.Channel.SendMessageAsync("You need to specify a melody name");
            return;
        }

        List<MusicInfo> info = Variables._MusicDatabase.GetMusicInfoWithTitleOrAlias(title);
        if (!info.Any())
        {
            args.Context.Channel.SendMessageAsync("No melody with that name or alias was found");
            return;
        }

        args.Context.Channel.SendMessageAsync(embed: info.Count > 1 ? info.ToEmbed(Color.DarkOrange) : info[0].ToEmbed(Color.DarkOrange));
    }
}
