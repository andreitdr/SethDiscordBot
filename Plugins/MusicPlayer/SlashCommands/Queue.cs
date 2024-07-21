using Discord;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;

namespace MusicPlayer.SlashCommands;

public class Queue: DBSlashCommand
{
    public string Name => "queue";
    public string Description => "Queue a melody to play";
    public bool canUseDM => false;
    public bool HasInteraction => false;
    public List<SlashCommandOptionBuilder> Options => null;


    public async void ExecuteServer(SocketSlashCommand context)
    {
        if (Variables._MusicPlayer is null)
        {
            await context.RespondAsync("No music is currently playing.");
            return;
        }

        if (Variables._MusicPlayer.MusicQueue.Count == 0 && Variables._MusicPlayer.CurrentlyPlaying == null)
        {
            await context.RespondAsync("No music is currently playing");
            return;
        }

        var builder = new EmbedBuilder()
        {
            Title       = "Music Queue",
            Description = "Here is the current music queue",
            Color       = Color.Blue
        };

        if (Variables._MusicPlayer.CurrentlyPlaying != null)
            builder.AddField("Current music", Variables._MusicPlayer.CurrentlyPlaying.Title);

        var i = 1;
        foreach (var melody in Variables._MusicPlayer.MusicQueue)
        {
            builder.AddField($"#{i}", melody.Title);
            i++;
        }

        await context.RespondAsync(embed: builder.Build());
    }
}
