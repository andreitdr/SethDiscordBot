using CMD_Utils.Music;

using PluginManager.Others;

using System;
using System.IO;
using System.Threading.Tasks;

namespace MusicCommands
{
    class MusicPlayer
    {
        public Stream inputStream { get; private set; }      // from FFMPEG
        public Stream outputStream { get; private set; }     // to Voice Channel 
        public MusicPlayer(Stream input, Stream output)
        {
            inputStream = input;
            outputStream = output;
        }

        public bool Paused { get; set; }
        private bool _stop { get; set; }
        public void Stop()
        {
            _stop = true;
        }

        public async Task StartSendAudio()
        {
            Paused = false;
            _stop = false;
            while (!_stop)
            {
                if (Paused) continue;
                int bsize = 512;
                byte[] buffer = new byte[bsize];
                var bcount = await inputStream.ReadAsync(buffer, 0, bsize);
                if (bcount <= 0)
                {
                    Stop();
                    Data.CurrentlyRunning = null;
                    break;
                }
                try
                {
                    await outputStream.WriteAsync(buffer, 0, bcount);
                }
                catch (Exception ex)
                {
                    await outputStream.FlushAsync();
                    Functions.WriteLogFile(ex.ToString());
                }

            }
        }
    }
}
