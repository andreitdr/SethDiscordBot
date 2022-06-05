using Avalonia.Controls;
using PluginManager.Online;
using PluginManager.Others;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Threading;
using PluginManager;

namespace DiscordBotGUI
{
    public partial class AppUpdater : Window
    {
        private string _version;

        public AppUpdater()
        {
            InitializeComponent();
            if (!File.Exists("./Version.txt"))
            {
                File.WriteAllText("./Version.txt", "DiscordBotVersion=0");
                DownloadDiscordBotClientNoGUIAsDLL();
            }

            if (!File.Exists("./DiscordBot.exe")) DownloadDiscordBotClientNoGUIAsDLL();
            Updates();
        }

        private async void DownloadDiscordBotClientNoGUIAsDLL()
        {
            //await Task.Delay(5000);
            string url_bot_dll = "https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Updates/DiscordBot.zip";
            int    actiontype  = 0; //0 - downolad, 1- extract
            IProgress<float> progress = new Progress<float>((percent) =>
            {
                if (actiontype == 0)
                    textBox1.Text = "Downloading DiscordBot ... " + MathF.Round(percent, 2) + "%";
                else
                    textBox1.Text = "Extracting package ..." + MathF.Round(percent, 2) + "%";
                this.progressBar1.Value = percent;
            });

            this.progressBar1.IsIndeterminate = false;
            try
            {
                await ServerCom.DownloadFileAsync(url_bot_dll, "./DiscordBot.zip", progress);

                actiontype++;

                await Functions.ExtractArchive("./DiscordBot.zip", "./", progress);
            }
            catch
            {
                textBox1.Text = "Error downloading DiscordBot.dll. Server is not responding.";

                await Task.Delay(1000);

                new MainWindow() { Height = 425, Width = 500 }.Show();
                Close();
            }
        }

        private async void Updates()
        {
            this.progressBar1.IsIndeterminate = true;
            await Task.Delay(1000);
            if (!await CheckForUpdates())
            {
                //await Task.Delay(5000);
                textBox1.Text = $"You are running on the latest version ({_version}) !";
                await Task.Delay(2000);
                new MainWindow() { Height = 425, Width = 650 }.Show();
                this.Close();
                return;
            }

            string file = await DownloadNewUpdate();
            if (file == null)
            {
                textBox1.Text = "There was an error while downloading the update !";
                await Task.Delay(2000);
                new MainWindow() { Height = 425, Width = 650 }.Show();
                this.Close();
                return;
            }

            IProgress<float> progress = new Progress<float>((percent) => { this.progressBar1.Value = percent; });

            textBox1.Text = "Extracting update files ...";
            await Functions.ExtractArchive(file, "./", progress);
            progressBar1.IsIndeterminate = true;
            textBox1.Text                = "Setting up the new version ...";
            File.Delete(file);
            File.WriteAllText("./Version.txt", "DiscordBotVersion=" + _version);
            await Task.Delay(5000);
            new MainWindow() { Height = 425, Width = 650 }.Show();
            this.Close();
        }

        private async Task<string> DownloadNewUpdate()
        {
            string urlNewUpdateZip = (await ServerCom.ReadTextFromFile("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Updates/Version"))[1];
            int    secondsPast     = 0;

            bool isDownloading = true;
            this.progressBar1.IsIndeterminate = true;
            textBox1.Text                     = "Downloading update ...";


            IProgress<long> downloaded = new Progress<long>((bytes) =>
            {
                (double, string) download = Functions.ConvertBytes(bytes);
                textBox1.Text = $"Downloading update ... {Math.Round(download.Item1 / secondsPast, 2)} {download.Item2}/s";
            });
            IProgress<float> progress = new Progress<float>((percent) =>
            {
                progressBar1.IsIndeterminate = false;
                this.progressBar1.Value      = percent;
            });


            string FileName = $"{urlNewUpdateZip.Split('/')[urlNewUpdateZip.Split('/').Length - 1]}";
            try
            {
                new Thread(new Task(() =>
                {
                    while (isDownloading)
                    {
                        Thread.Sleep(1000);
                        secondsPast++;
                    }
                }).Start).Start();
                await ServerCom.DownloadFileAsync(urlNewUpdateZip, FileName, progress, downloaded);
            }
            catch
            {
                textBox1.Text = "Error downloading the update. Server is not responding.";
                isDownloading = false;
                await Task.Delay(1000);
                return null;
            }

            isDownloading = false;
            return FileName;
        }

        private async Task<bool> CheckForUpdates()
        {
            try
            {
                string current_version = Config.GetValue("Version");
                if (current_version == null)
                    if (!Config.SetValue("Version", "0"))
                        Config.AddValueToVariables("Version", "0", false);
                string latest_version  = (await ServerCom.ReadTextFromFile("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Updates/Version"))[0];
                _version = latest_version;
                if (current_version != latest_version) { return true; }

                return false;
            }
            catch (Exception ex)
            {
                textBox1.Text = "Error while checking for updates. Server is not responding.";
                Functions.WriteErrFile(ex.Message);
                return false;
            }
        }
    }
}
