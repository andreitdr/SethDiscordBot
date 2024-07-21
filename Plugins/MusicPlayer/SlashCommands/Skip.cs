using Discord;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;

namespace MusicPlayer.SlashCommands;

public class Skip: DBSlashCommand
{
    public string Name => "skip";
    public string Description => "Skip the current melody";
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

        var melodyTitle = Variables._MusicPlayer.CurrentlyPlaying.Title;

        await context.RespondAsync($"Skipping {melodyTitle} ...");
        Variables._MusicPlayer.Skip();
        await context.ModifyOriginalResponseAsync(x => x.Content = $"Skipped {melodyTitle}");
    }
}
