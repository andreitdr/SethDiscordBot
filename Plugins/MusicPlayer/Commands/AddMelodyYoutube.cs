using System.Diagnostics;

using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace MusicPlayer.Commands;

public class AddMelodyYoutube: IDbCommand
{
    public string Command => "add_melody_youtube";

    public List<string>? Aliases => new()
    {
        "madd-yt"
    };

    public string Description => "Add melody to the database from a youtube link";
    public string Usage => "add_melody_youtube [URL] <alias1|alias2|...>";
    public bool RequireAdmin => true;

    public async void ExecuteServer(DbCommandExecutingArguments args)
    {
        
        if(Variables._MusicDatabase is null)
        {
            await args.Context.Channel.SendMessageAsync("Music Database is not loaded !");
            return;
        }
        
        if (args.Arguments is null)
        {
            await args.Context.Channel.SendMessageAsync("Invalid arguments given. Please use the following format:\nadd_melody_youtube [URL]");
            return;
        }


        var url = args.Arguments[0];

        if (!url.StartsWith("https://www.youtube.com/watch?v=") && !url.StartsWith("https://youtu.be/"))
        {
            await args.Context.Channel.SendMessageAsync("Invalid URL given. Please use the following format:\nadd_melody_youtube [URL]");
            return;
        }

        if (args.Arguments.Length <= 1)
        {
            await args.Channel.SendMessageAsync("Please specify at least one alias for the melody !");
            return;
        }

        var msg = await args.Context.Channel.SendMessageAsync("Saving melody ...");

        var title = await YoutubeDLP.DownloadMelody(url);

        if (title == null)
        {
            await msg.ModifyAsync(x => x.Content = "Failed to download melody !");
            return;
        }

        var          joinedAliases = string.Join(" ", args.Arguments.Skip(1));
        List<string> aliases       = joinedAliases.Split('|').ToList();


        if (Variables._MusicDatabase.GetMusicInfoWithTitleOrAlias(title).Any())
        {
            await msg.ModifyAsync(x => x.Content = "Melody already exists !");
            return;
        }

        Variables._MusicDatabase.Add(title, new MusicInfo()
            {
                Aliases     = aliases,
                ByteSize    = 1024,
                Description = "Melody added from youtube link",
                Location    = Application.GetResourceFullPath($"Music/Melodies/{title}.mp3"),
                Title       = title
            }
        );



        await Variables._MusicDatabase.SaveToFile();
        await msg.ModifyAsync(x => x.Content = "Melody saved !");
    }
}
