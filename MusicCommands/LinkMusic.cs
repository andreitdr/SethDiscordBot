using Discord.Net;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace MusicCommands
{
    public class LinkMusic
    {
        private string URL;
        public LinkMusic(string URL)
        {
            this.URL = URL;
        }
        public async Task<Stream> GetMusicStreamAsync()
        {
            WebClient client = new WebClient();
            return await client.OpenReadTaskAsync(this.URL);
        }
    }
}
