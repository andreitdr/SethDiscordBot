using System.Diagnostics;
using System.Text.RegularExpressions;

namespace MusicPlayer;

public class YoutubeDlp
{
    public static async Task DownloadMelody(string url, string downloadId)
    {
        var    process            = new Process();
        var baseMusicDirectory = DiscordBotCore.Application.GetResourceFullPath("Music/Melodies/");
        
        process.StartInfo.FileName  = await DiscordBotCore.Application.CurrentApplication.PluginManager.GetDependencyLocation("yt-dlp");
        process.StartInfo.Arguments = $"-x --force-overwrites -o \"{baseMusicDirectory}/{downloadId}.%(ext)s\" --audio-quality 3 --audio-format mp3 {url}";

        process.StartInfo.RedirectStandardOutput = true;
        process.OutputDataReceived += (_, args) =>
        {
            if (args.Data != null)
            {
                Console.WriteLine(args.Data);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        await process.WaitForExitAsync();
    }
    
    
    public static async Task<string?> GetMusicTitle(string youtubeUrl)
    {
        Process p = new Process();
        p.StartInfo.FileName=await DiscordBotCore.Application.CurrentApplication.PluginManager.GetDependencyLocation("yt-dlp");
        p.StartInfo.Arguments = $"--print \"%(title)s\" {youtubeUrl}";
        p.StartInfo.RedirectStandardOutput = true;
        
        p.Start();
        string output = await p.StandardOutput.ReadToEndAsync();
        await p.WaitForExitAsync();
        
        output = Regex.Replace(output, @"[\p{L}-[a-zA-Z]]+", "");
        output = output.TrimEnd();

        return output;
    }
}
