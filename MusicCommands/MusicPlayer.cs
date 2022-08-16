using System.IO;
using System.Threading.Tasks;

namespace MusicCommands;

internal class MusicPlayer
{
    private  Stream    outputStream { get; }
    internal AudioFile NowPlaying = null;

    internal bool isPlaying, isPaused;

    public MusicPlayer(Stream outputChannel)
    {
        outputStream = outputChannel;
    }

    public async Task Play(Stream source, int byteSize, AudioFile songPlaying)
    {
        isPlaying  = true;
        NowPlaying = songPlaying;
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

    public void Stop()
    {
        isPlaying = false;
    }
}
