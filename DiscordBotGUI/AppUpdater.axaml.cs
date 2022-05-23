using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using PluginManager.Online;
using PluginManager.Others;
using System.Threading.Tasks;
using System;
using System.IO;

namespace DiscordBotGUI
{
    public partial class AppUpdater : Window
    {
        public AppUpdater()
        {
            InitializeComponent();
            if (!File.Exists("./Version.txt"))
            {
                textBox1.Text = "Checking ...";
                File.WriteAllText("./Version.txt", "DiscordBotVersion=0");
                DownloadDiscordBotClientNoGUIAsDLL();
            }

            Updates();

        }

        private async void DownloadDiscordBotClientNoGUIAsDLL()
        {

            //await Task.Delay(5000);
            string url_bot_dll = "https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Updates/DiscordBot.dll";
            IProgress<float> progress = new Progress<float>((percent) =>
            {
                textBox1.Text = "Downloading DiscordBot.dll ... " + percent.ToString() + "%";
                this.progressBar1.Value = percent * 100;
            });

            this.progressBar1.IsIndeterminate = false;
            await ServerCom.DownloadFileAsync(url_bot_dll, "./DiscordBot.dll", progress);
            new MainWindow().Show();
            Close();
        }

        private async void Updates()
        {
            if (!await CheckForUpdates())
            {
                await Task.Delay(5000);
                textBox1.Text = "There is no update found !";
                await Task.Delay(2000);
                new MainWindow().Show();
                this.Close();
                return;
            }

            string file = await DownloadNewUpdate();
            if (file == null)
            {
                textBox1.Text = "There was an error while downloading the update !";
                await Task.Delay(5000);
                new MainWindow().Show();
                this.Close();
                return;
            }

            IProgress<float> progress = new Progress<float>((percent) =>
            {
                this.progressBar1.Value = percent;
            });
            await Functions.ExtractArchive(file, "./", progress);


            textBox1.Text = "Update downloaded successfully !";
            await Task.Delay(2000);
            new MainWindow().Show();
            this.Close();

        }

        private async Task<string> DownloadNewUpdate()
        {
            string urlNewUpdateZip = (await ServerCom.ReadTextFromFile("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Updates/Version"))[1];

            IProgress<float> progress = new Progress<float>((percent) =>
            {
                this.progressBar1.Value = percent;
            });

            this.progressBar1.IsIndeterminate = false;
            string FileName = $"{urlNewUpdateZip.Split('/')[urlNewUpdateZip.Split('/').Length - 1]}.zip";
            await ServerCom.DownloadFileAsync(urlNewUpdateZip, FileName, progress);

            return FileName;
        }

        private async Task<bool> CheckForUpdates()
        {
            try
            {

                string current_version = Functions.readCodeFromFile("Version.txt", "DiscordBotVersion", '=') ?? "0";
                string latest_version = (await ServerCom.ReadTextFromFile("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Updates/Version"))[0];
                if (current_version != latest_version)
                {
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                //File.WriteAllText("./Debug.txt", "Error while checking for updates !\n" + ex.ToString());
                return false;
            }
        }
    }
}
