using AngleSharp.Dom;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MusicCommands
{
    internal class AudioFile
    {
        internal string Name { get; set; }
        internal string Url  { get; set; }

        internal AudioFile(string name, string url)
        {
            Name = name;
            Url  = url;
        }

        internal async Task DownloadAudioFile()
        {
            Process proc = new Process();
            proc.StartInfo.FileName               = "MusicDownloader.exe";
            proc.StartInfo.Arguments              = $"{Url},{Name}";
            proc.StartInfo.UseShellExecute        = false;
            proc.StartInfo.RedirectStandardOutput = true;

            proc.Start();
            await proc.WaitForExitAsync();
        }
    }
}
