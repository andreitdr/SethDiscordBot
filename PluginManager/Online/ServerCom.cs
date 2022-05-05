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
        public static async Task DownloadFileAsync(string URL, string location, IProgress<float> progress, IProgress<long> downloadedBytes)
        {
            using (var client = new System.Net.Http.HttpClient())
            {
                client.Timeout = TimeSpan.FromMinutes(5);

                using (var file = new FileStream(location, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    await client.DownloadFileAsync(URL, file, progress, downloadedBytes);
                }
            }
        }

        /// <summary>
        /// Download file from url
        /// </summary>
        /// <param name="URL">The url to the file</param>
        /// <param name="location">The location where to store the downloaded data</param>
        /// <returns></returns>
        public static async Task DownloadFileAsync(string URL, string location)
        {
            bool isDownloading = true;
            int c_progress = 0;

            long secondsPast = 0;
            long m_dwBytes = 0;
            double c_downloadSpeed = 0f;
            string c_downloadUnit = "MB";

            Others.Console_Utilities.ProgressBar pbar = new Others.Console_Utilities.ProgressBar(100, "");

            IProgress<float> progress = new Progress<float>(percent =>
            {
                c_progress = (int)percent;
            });

            IProgress<long> progress_downloaded = new Progress<long>(downloadedBytes =>
            {
                m_dwBytes = downloadedBytes;
            });

            Task updateProgressBarTask = new Task(async () =>
            {
                while (isDownloading)
                {
                    //pbar.Update(c_progress, c_downloadSpeed, c_downloadUnit);
                    pbar.Update(c_progress);
                    if (c_progress == 100)
                        break;
                    System.Threading.Thread.Sleep(500);
                }
            });

            Task calculateDownloadSpeed = new Task(async () =>
            {

                while (isDownloading)
                {
                    secondsPast++;
                    c_downloadSpeed = m_dwBytes / secondsPast;

                    c_downloadSpeed /= 1024; // in KB
                    c_downloadSpeed /= 1024; // in MB

                    c_downloadSpeed = Math.Round(c_downloadSpeed, 2, MidpointRounding.AwayFromZero);

                    System.Threading.Thread.Sleep(1000);
                }
            });

            // new System.Threading.Thread(calculateDownloadSpeed.Start).Start();
            new System.Threading.Thread(updateProgressBarTask.Start).Start();
            await DownloadFileAsync(URL, location, progress, progress_downloaded);

            isDownloading = false;
            c_progress = 100;
            pbar.Update(100);

        }
    }
}
