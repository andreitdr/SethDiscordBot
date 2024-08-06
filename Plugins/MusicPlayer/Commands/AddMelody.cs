using Discord;

using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Online;
using DiscordBotCore.Others;

namespace MusicPlayer.Commands;

public class AddMelody: IDbCommand
{
    public string Command => "add_melody";

    public List<string>? Aliases => new()
    {
        "madd"
    };

    public string Description => "Add a custom melody to the database";
    public string Usage => "add_melody [title],[description?],[aliases],[byteSize]";
    public bool RequireAdmin => false;

    public async void ExecuteServer(DbCommandExecutingArguments args)
    {
        var      arguments = string.Join(" ", args.Arguments);
        string[] split     = arguments.Split(',');

        if (split.Length < 4)
        {
            var message = "";
            message += "Invalid arguments given. Please use the following format:\n";
            message += "add_melody [title],[description?],[aliases],[byteSize]\n";
            message += "title: The title of the melody\n";
            message += "description: The description of the melody\n";
            message += "aliases: The aliases of the melody. Use | to separate them\n";
            message += "byteSize: The byte size of the melody. Default is 1024. ( & will use default)\n";

            await args.Context.Channel.SendMessageAsync(message);

            return;
        }

        if (args.Context.Message.Attachments.Count == 0)
        {
            await args.Context.Channel.SendMessageAsync("You must upload a valid .mp3 audio or .mp4 video file !!");
            return;
        }

        var file = args.Context.Message.Attachments.FirstOrDefault();
        if (!(file.Filename.EndsWith(".mp3") || file.Filename.EndsWith(".mp4")))
        {
            await args.Context.Channel.SendMessageAsync("Invalid file format !!");
            return;
        }


        var       title       = split[0];
        var       description = split[1];
        string[]? aliases     = split[2]?.Split('|') ?? null;
        var       byteSize    = split[3];
        int       bsize;
        if (!int.TryParse(byteSize, out bsize))
            bsize = 1024;


        var msg = await args.Context.Channel.SendMessageAsync("Saving melody ...");
        Console.WriteLine("Saving melody");

        IProgress<float> downloadProgress = new Progress<float>();

        
        var location = Application.GetResourceFullPath($"Music/Melodies/{title}.mp3");
        Directory.CreateDirectory(Application.GetResourceFullPath("Music/Melodies"));
        await ServerCom.DownloadFileAsync(file.Url, location, downloadProgress);

        Console.WriteLine($"Done. Saved at {location}");

        await msg.ModifyAsync(a => a.Content = "Done");


        var info = MusicInfoExtensions.CreateMusicInfo(title, location, description ?? "Unknown", aliases.ToList(), bsize);

        Variables._MusicDatabase?.Add(title, info);

        var builder = new EmbedBuilder();
        builder.Title = "A new music was successfully added !";
        builder.AddField("Title", info.Title);
        builder.AddField("Description", info.Description);
        builder.AddField("Aliases", string.Join(" | ", aliases));
        await args.Context.Channel.SendMessageAsync(embed: builder.Build());

        await Variables._MusicDatabase.SaveToFile();

    }
}
