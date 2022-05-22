using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using PluginManager.Others;

using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System;

namespace DiscordBotGUI.Settings
{
    public partial class Commands : Window
    {
        List<string> commands = new List<string>();
        public Commands()
        {
            InitializeComponent();
            LoadData();
            LoadComboBox();

            button1.Click += async (sender, e) =>
            {
                await Download();
            };
        }


        private void LoadData()
        {
            try
            {
                textbox1.Text = "";
                var files = System.IO.Directory.EnumerateFiles("./Data/Plugins/Commands/");
                if (files == null || files.Count() < 1) return;
                foreach (var file in files)
                    textbox1.Text += file.Split('/')[file.Split('/').Length - 1] + "\n";
            }
            catch { }
        }

        private async void LoadComboBox()
        {
            comboBox1.Items = null;
            commands = await PluginManager.Online.ServerCom.ReadTextFromFile("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Plugins");
            if (commands == null) return;
            string[] plugins = commands.ToArray();
            string OS;
            var OSG = Functions.GetOperatingSystem();
            if (OSG == PluginManager.Others.OperatingSystem.WINDOWS) OS = "Windows";
            else if (OSG == PluginManager.Others.OperatingSystem.LINUX) OS = "Linux";
            else OS = "MAC_OS";
            List<string> data = new List<string>();
            for (int i = 0; i < plugins.Length; i++)
            {
                if (!plugins[i].Contains(OS) || !plugins[i].Contains("Commands"))
                    continue;



                string[] info = plugins[i].Split(',');
                if (System.IO.Directory.EnumerateFiles("./Data/Plugins/Commands/").Any(x => x.EndsWith(info[0] + ".dll")))
                    continue;
                data.Add($"{info[0]} - {info[1]} - {info[2]}");
            }

            comboBox1.Items = data;
        }


        private async Task Download()
        {
            if (comboBox1 == null || comboBox1.SelectedIndex == -1 || comboBox1.SelectedItem == null)
                return;
            string? pluginName = comboBox1?.SelectedItem?.ToString()?.Split('-')[0].Trim();

            if (pluginName == null) return;
            string? URL = (from s in commands
                           where s.StartsWith(pluginName)
                           select s.Split(',')[3].Trim()).FirstOrDefault();

            if (URL == null) return;


            IProgress<float> progress = new Progress<float>(async value =>
            {
                label1.Content = $"Downloading {pluginName} {MathF.Round(value, 2)}%";
                if (value == 1f)
                {
                    label1.Content = "Successfully Downloaded " + pluginName;
                    LoadData();
                    LoadComboBox();

                    await Task.Delay(5000);
                    label1.Content = "";
                }
                progressBar1.Value = value;
            });

            await PluginManager.Online.ServerCom.DownloadFileAsync(URL, "./Data/Plugins/Commands/" + pluginName + ".dll", progress);
            string? requirements = (from s in commands
                                    where s.StartsWith(pluginName) && s.Split(',').Length == 6
                                    select s.Split(',')[5].Trim()).FirstOrDefault();

            if (requirements == null) return;
            List<string> req = await PluginManager.Online.ServerCom.ReadTextFromFile(requirements);
            if (req == null) return;

            foreach (var requirement in req)
            {
                string[] info = requirement.Split(',');
                pluginName = info[1];
                progress.Report(0);
                await PluginManager.Online.ServerCom.DownloadFileAsync(info[0], $"./{info[1]}", progress);

                await Task.Delay(1000);

                if (info[0].EndsWith(".zip"))
                {
                    await Functions.ExtractArchive("./" + info[1], "./", progress);
                    await Task.Delay(1000);
                }

            }

            progress.Report(100f);
            label1.Content = "Downloaded";
            progressBar1.Value = 100;
        }
    }
}
