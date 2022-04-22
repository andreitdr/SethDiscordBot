using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

using PluginManager.Items;

namespace PluginManager.Online
{
    public class Downloader
    {
        public bool isWorking { get; private set; }

        public int percent { get; private set; }
        private string fileName;
        private string downloadLink;

        public Downloader(string fileName, string fileLink)
        {
            this.downloadLink = fileLink;
            this.fileName = fileName;
        }


        public async Task DownloadFileAsync(string location = @"./Downloads/", string? pluginType = null, string? customMessage = null)
        {
            if (customMessage != null)
                Console.WriteLine(customMessage);

            Directory.CreateDirectory(location);
            if (isWorking) return;
            isWorking = true;
            percent = 0;

            Spinner s = new Spinner();
            Console.Write("Downloading:\t\t");
            s.Start();

#pragma warning disable SYSLIB0014
            WebClient client = new WebClient();
#pragma warning restore SYSLIB0014

            client.DownloadFileCompleted += (sender, args) =>
            {
                isWorking = false;
                s.Stop();
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.DarkGreen;
                Console.Write("OK");
                Console.ForegroundColor = c;
                Console.Write(" !\n");
                //Console.WriteLine("Your plugin has been successfully downloaded !");
                if (pluginType == "Event/Command" || pluginType == "Command/Event")
                {
                    File.Copy(location + fileName, location + "Commands/" + fileName, true);
                    File.Move(location + fileName, location + "Events/" + fileName, true);
                }

            };

            string l = "";
            if (pluginType == "Command")
                l = location + "Commands/" + fileName;
            else if (pluginType == "Event")
                l = location + "Events/" + fileName;
            else l = location + fileName;
            try
            {
                await client.DownloadFileTaskAsync(new Uri(this.downloadLink), l);
            }
            catch
            {
                var c = Console.ForegroundColor;
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write("FAIL");
                Console.ForegroundColor = c;
                Console.Write(" !\n");
            }
        }

    }
}
