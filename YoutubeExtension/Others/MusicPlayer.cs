namespace YoutubeExtension.Downloader;

internal class MusicPlayer
{
    public MusicPlayer(Stream input, Stream output)
    {
        inputStream  = input;
        outputStream = output;
    }

    public Stream inputStream  { get; } // from outside
    public Stream outputStream { get; } // to Voice Channel 

    public  bool Paused { get; set; }
    private bool _stop  { get; set; }

    public void Stop()
    {
        _stop = true;
    }

    public async Task StartSendAudio()
    {
        Paused = false;
        _stop  = false;
        while (!_stop)
        {
            if (Paused) continue;
            var bsize  = 512;
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
                PluginManager.Others.Functions.WriteLogFile(ex.ToString());
            }
        }
    }
}
