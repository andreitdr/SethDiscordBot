using Discord.WebSocket;
using DiscordBotCore;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;

namespace MusicPlayer.Events;

public class OnVoiceRemoved: DBEvent
{

    public string Name => "Event: OnVoiceRemoved";
    public string Description => "Called when bot leaves a voice channel";

    public void Start(DiscordSocketClient client)
    {
        client.UserVoiceStateUpdated += async (user, oldState, newState) =>
        {
            if (user.Id == client.CurrentUser.Id && newState.VoiceChannel == null)
            {
                Variables._MusicPlayer?.MusicQueue.Clear();
                Variables._MusicPlayer?.Skip();
                Variables._MusicPlayer?.Stop();
                await Variables.audioClient!.StopAsync();
                Variables.audioClient  = null;
                Variables._MusicPlayer = null;

                Application.CurrentApplication.Logger.Log("Bot left voice channel.", this, LogType.Info);
            }

        };
    }

}
