using PluginManager.Others.Exceptions;

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
namespace MusicCommands
{
    public class LinkMusic
    {
        public string URL { get; private set; }
        public LinkType type { get; private set; }


        public LinkMusic(string URL)
        {
            this.URL = URL;
            if (URL.StartsWith("https://www.youtube.com/watch?v="))
                type = LinkType.YOUTUBE;
            else if (URL.StartsWith("https://open.spotify.com/track/"))
                type = LinkType.SPOTIFY;
            else type = LinkType.RAW;
        }

        private async Task<string> GetYoutubeVideoID()
        {
            //https://www.youtube.com/watch?v=i-p--m7qaCM&ab_channel={channel}/
            return URL.Split('=')[1].Split('&')[0];
        }

        public async Task<Stream> GetStream()
        {
            Stream s;
            if (type == LinkType.SPOTIFY) s = await GetSpotifyMusicStreamAsync();
            else if (type == LinkType.YOUTUBE) s = await GetYoutubeMusicStreamAsync();
            else s = await GetRAWMusicStreamAsync();
            if (s == null)
            {
                Console.WriteLine("Failed to get the stream for url: " + this.URL);
                throw new APIException("URL [" + this.URL + "] is invalid", "async Task <Stream> GetStream()", "Work in progress", PluginManager.Others.Error.STREAM_NOT_FOUND);
            }
            return s;
        }

        private async Task<Stream> GetSpotifyMusicStreamAsync()
        {
            throw new APIException("Not implemented", "async Task<Stream> GetSpotifyMusicStream()",
                                   "Work in progress", PluginManager.Others.Error.STREAM_NOT_FOUND);
        }

        private async Task<Stream> GetYoutubeMusicStreamAsync()
        {
            //https://www.youtube.com/get_video_info?video_id={id}&el=detailpage
            string ID = await GetYoutubeVideoID();


            using (var webc = new WebClient())
            {
                Stream s = await webc.OpenReadTaskAsync($"https://www.youtube.com/get_video_info?video_id={ID}&el=detailpage");
                string str = await new StreamReader(s).ReadToEndAsync();
                Console.WriteLine(str);

                await Task.Delay(-1);
            }
            return null;
        }

        private async Task<Stream> GetRAWMusicStreamAsync()
        {
            return await new WebClient().OpenReadTaskAsync(URL);
        }
    }
}
