using Discord;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;

namespace MusicPlayer.SlashCommands;

public class Loop: DBSlashCommand
{

    public string Name => "loop";
    public string Description => "Loop the current song for a certain amount of times. If no times are specified, it will loop once";
    public bool canUseDM => false;
    public bool HasInteraction => false;

    public List<SlashCommandOptionBuilder> Options => new()
    {
        new()
        {
            Type        = ApplicationCommandOptionType.Integer,
            Name        = "times",
            Description = "How many times to loop the song",
            IsRequired  = false
        }
    };


    public void ExecuteServer(SocketSlashCommand context)
    {
        if (Variables._MusicPlayer.CurrentlyPlaying == null)
        {
            context.RespondAsync("There is nothing playing right now");
            return;
        }

        var times = context.Data.Options.FirstOrDefault()?.Value.ToString() ?? "1";

        if (!int.TryParse(times, out var timesToLoop))
        {
            context.RespondAsync("Invalid number");
            return;
        }

        if (timesToLoop < 1)
        {
            context.RespondAsync("You need to specify a number greater than 0");
            return;
        }

        Variables._MusicPlayer.Loop(timesToLoop);

        context.RespondAsync($"Looping {Variables._MusicPlayer.CurrentlyPlaying.Title} {timesToLoop} times. Check the queue to see the progress");

    }
}
