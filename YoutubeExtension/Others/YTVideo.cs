using MediaToolkit;
using MediaToolkit.Model;
using VideoLibrary;

namespace YoutubeExtension.Downloader;

public class YoutubeHandler
{

    public static async Task<Stream> GetVideoStream(string videoURL)
    {
        var youtube = YouTube.Default;
        var video   = await youtube.GetVideoAsync(videoURL);

        return await video.StreamAsync();

    }
}
