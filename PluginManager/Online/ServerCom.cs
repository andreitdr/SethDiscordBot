using PluginManager.Items;
using PluginManager.Online.Helpers;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace PluginManager.Online
{
    public class ServerCom
    {

        /// <summary>
        /// Read all lines from a file async
        /// </summary>
        /// <param name="link">The link of the file</param>
        /// <returns></returns>
        public static async Task<List<string>> ReadTextFromFile(string link)
        {
            string response = await OnlineFunctions.DownloadStringAsync(link);
            string[] lines = response.Split('\n');
            return lines.ToList();
        }

        /// <summary>
        /// Download file from url
        /// </summary>
        /// <param name="URL">The url to the file</param>
        /// <param name="location">The location where to store the downloaded data</param>
        /// <param name="progress">The <see cref="IProgress{T}"/> to track the download</param>
        /// <returns></returns>
        public static async Task DownloadFileAsync(string URL, string location, IProgress<float> progress)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using (var file = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadFileAsync(URL, file, progress);
                }
            }
        }
    }
}
