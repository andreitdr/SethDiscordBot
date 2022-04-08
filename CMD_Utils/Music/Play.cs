using Discord;
using Discord.Audio;
using Discord.Commands;
using Discord.WebSocket;

using Microsoft.VisualBasic;

using PluginManager.Interfaces;
using PluginManager.Others;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace CMD_Utils.Music
{
    class Play : DBCommand
    {
        public string Command => "fplay";

        public string Description => "Play music from a file";

        public string Usage => "fplay [name]";

        public bool canUseDM => false;

        public bool canUseServer => true;

        public bool requireAdmin => false;

        public async void Execute(SocketCommandContext context, SocketMessage message, DiscordSocketClient client, bool isDM)
        {
            string path = "./Music";
            string FileName = Functions.GetArguments(message).ToArray().MergeStrings(0);
            path += "/" + FileName + ".mp3";
            if (!File.Exists(path))
            {
                Console.WriteLine("Unknown path " + path);
                return;
            }

            // Get the audio channel

            Data.voiceChannel = (context.User as IGuildUser)?.VoiceChannel;
            if (Data.voiceChannel == null) { await context.Channel.SendMessageAsync("User must be in a voice channel, or a voice channel must be passed as an argument."); return; }

            // For the next step with transmitting audio, you would want to pass this Audio Client in to a service.
            Data.audioClient = await Data.voiceChannel.ConnectAsync();


            // Create FFmpeg using the previous example
            using (var ffmpeg = CreateStream(path))
            using (var output = ffmpeg.StandardOutput.BaseStream)
            using (var discord = Data.audioClient.CreatePCMStream(AudioApplication.Mixed))
            {
                try { await output.CopyToAsync(discord); }
                finally { await discord.FlushAsync(); }
            }
        }

        private Process CreateStream(string path)
        {
            return Process.Start(new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = $"-hide_banner -loglevel panic -i \"{path}\" -ac 2 -f s16le -ar 48000 pipe:1",
                UseShellExecute = false,
                RedirectStandardOutput = true,
            });
        }
    }
}
