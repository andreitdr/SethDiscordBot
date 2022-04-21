using PluginManager.Items;

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
            List<string> s = new List<string>();
            WebClient webClient = new WebClient();
            var data = await webClient.OpenReadTaskAsync(link);
            var response = await new StreamReader(data).ReadToEndAsync();
            s.AddRange(from a in response.Split('\n')
                       where !a.StartsWith("$")
                       select a);
            return s;
        }

        public static async Task DownloadFileAsync(string url, string location, int downloadNumber, int totalToDownload)
        {
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
            }

        }
    }
}
