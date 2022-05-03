using CMD_Utils.Music;

using PluginManager.Others;

using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
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

        public MusicPlayer(Stream output)
        {
            inputStream = null;
            outputStream = output;
        }

        public bool Paused { get; set; }
        private bool _stop { get; set; }
        public void Stop()
        {
            _stop = true;
        }

        public async Task StartSendAudioFromLink(string URL)
        {
            Stream ms = new MemoryStream();
            int bsize = 512;
            new Thread(async delegate (object o)
                    {
                        var response = await new HttpClient().GetAsync(URL);
                        using (var stream = await response.Content.ReadAsStreamAsync())
                        {
                            byte[] buffer = new byte[bsize];
                            int read;
                            while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                            {
                                var pos = ms.Position;
                                ms.Position = ms.Length;
                                ms.Write(buffer, 0, read);
                                ms.Position = pos;
                            }
                        }
                    }).Start();
            Console.Write("Reading data: ");
            while (ms.Length < bsize * 10)
            {
                await Task.Delay(1000);
                Console.Title = "Reading data: " + ms.Length + " bytes read of " + bsize * 10;
                Console.Write(".");
            }
            Console.WriteLine("\nDone");
            ms.Position = 0;

            _stop = false;
            Paused = false;

            while (!_stop)
            {
                if (Paused) continue;
                byte[] buffer = new byte[bsize];
                int read = await ms.ReadAsync(buffer, 0, buffer.Length);
                if (read > 0)
                {
                    await outputStream.WriteAsync(buffer, 0, read);
                }
                else
                {
                    break;
                }
            }


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
