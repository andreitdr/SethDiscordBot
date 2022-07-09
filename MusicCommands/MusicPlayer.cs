using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using PluginManager.Others;

namespace MusicCommands;

internal class MusicPlayer
{
    private Stream outputStream { get; }

    internal bool isPlaying, isPaused;

    public MusicPlayer(Stream outputChannel)
    {
        outputStream = outputChannel;
    }

    public async Task Play(Stream source, int byteSize)
    {
        isPlaying = true;
        while (isPlaying)
        {
            if (isPaused)
                continue;

            var bits = new byte[byteSize];
            var read = await source.ReadAsync(bits, 0, byteSize);
            if (read == 0)
                break;
            try
            {
                await outputStream.WriteAsync(bits, 0, read);
            }
            catch
            {
                break;
            }
        }


        await source.FlushAsync();
        await source.DisposeAsync();
        source.Close();
        await outputStream.FlushAsync();
        isPlaying = false;
    }


    /*
        public MusicPlayer(Stream input, Stream output)
        {
            inputStream  = input;
            outputStream = output;
        }





        public Stream inputStream  { get; } // from FFMPEG
        public Stream outputStream { get; } // to Voice Channel 

        public  bool Paused { get; set; }
        private bool _stop  { get; set; }

        public void Stop()
        {
            _stop = true;
        }

        public async Task StartSendAudio(int bsize)
        {
            Paused = false;
            _stop  = false;
            while (!_stop)
            {
                if (Paused) continue;
                var buffer = new byte[bsize];
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
        }*/
}
