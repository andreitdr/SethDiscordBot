using Discord;
using Discord.WebSocket;

using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace MusicPlayer.SlashCommands;

public class Play: IDbSlashCommand
{
    public string Name => "play";
    public string Description => "Play music command";
    public bool CanUseDm => false;
    public bool HasInteraction => false;

    public List<SlashCommandOptionBuilder> Options => new()
    {
        new()
        {
            IsRequired  = true,
            Description = "The music name to be played",
            Name        = "music-name",
            Type        = ApplicationCommandOptionType.String
        }
    };

    

    public async void ExecuteServer(SocketSlashCommand context)
    {
        
        if(Variables._MusicDatabase is null)
        {
            await context.RespondAsync("Music Database is not loaded !");
            return;
        }
        
        var melodyName = context.Data.Options.First().Value as string;

        if (melodyName is null)
        {
            await context.RespondAsync("Failed to retrieve melody with name " + melodyName);
            return;
        }

        var melody = Variables._MusicDatabase.GetMusicInfoWithTitleOrAlias(melodyName);
        if (!melody.Any())
        {
            await context.RespondAsync("The searched melody does not exists in the database. Sorry :(");
            return;
        }

        var user = context.User as IGuildUser;
        if (user is null)
        {
            await context.RespondAsync("Failed to get user data from channel ! Check error log at " + DateTime.Now.ToLongTimeString());
            Application.Logger.Log("User is null while trying to convert from context.User to IGuildUser.", typeof(Play), LogType.Error);
            return;
        }

        var voiceChannel = user.VoiceChannel;
        if (voiceChannel is null)
        {
            await context.RespondAsync("Unknown voice channel. Maybe I do not have permission to join it ?");
            return;
        }

        if (Variables.audioClient is null)
        {
            Variables.audioClient = await voiceChannel.ConnectAsync(true); // self deaf
        }

        Variables._MusicPlayer ??= new MusicPlayer();

        if (!Variables._MusicPlayer.Enqueue(melodyName))
        {
            await context.RespondAsync("Failed to enqueue your request. Something went wrong !");
            return;
        }
        
        await context.RespondAsync("Enqueued your request");

        await Variables._MusicPlayer.PlayQueue(); //start queue
    }
}
