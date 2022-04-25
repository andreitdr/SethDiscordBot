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
        public static async Task<List<string>> ReadTextFromFile(string link)
        {
            string response = await OnlineFunctions.DownloadStringAsync(link);
            string[] lines = response.Split('\n');
            return lines.ToList();


            //[Obsolete]
            #region old code for reading text from link
            /*
            List<string> s = new List<string>();
            WebClient webClient = new WebClient();
            var data = await webClient.OpenReadTaskAsync(link);
            var response = await new StreamReader(data).ReadToEndAsync();
            s.AddRange(from a in response.Split('\n')
                       where !a.StartsWith("$")
                       select a);
            return s;*/
            #endregion
        }

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

        public static async Task DownloadFileAsync(string url, string location, int downloadNumber, int totalToDownload)
        {

            IProgress<float> progress = new Progress<float>(bytes =>
            {
                Console.Title = $"Downloading {MathF.Round(bytes, 2)}% ({downloadNumber}/{totalToDownload})";
            });

            await DownloadFileAsync(url, location, progress);
            Console.Title = "ONLINE";
            return;

            //[Obsolete]
            #region old download code
            /*
            WebClient client = new WebClient();
            Spinner spinner = new Spinner();
            Console.Write("Downloading ");
            spinner.Start();
            string oldTitle = Console.Title ?? "";
            client.DownloadProgressChanged += (sender, e) =>
            {
                Console.Title = e.BytesReceived + "/" + e.TotalBytesToReceive + " (" + e.ProgressPercentage + "%) (" + downloadNumber + " / " + totalToDownload + ")";
            };
            client.DownloadFileCompleted += (sender, e) =>
            {
                spinner.Stop(); Console.WriteLine();

            };
            try
            {
                await client.DownloadFileTaskAsync(new Uri(url), location);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.Title = oldTitle;
            }*/

            #endregion
        }
    }
}
