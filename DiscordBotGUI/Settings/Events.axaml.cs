using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using PluginManager.Others;

using System.Collections.Generic;
using System;
using System.Linq;
using System.Reflection.Emit;
using System.Threading.Tasks;

using static PluginManager.Others.Console_Utilities;
using System.IO;

namespace DiscordBotGUI.Settings
{
    public partial class Events : Window
    {
        List<string> events = new List<string>();
        public Events()
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
            //Read components from Commands Folder:
            //textbox1 = new TextBox();
            try
            {
                Directory.CreateDirectory("./Data/Plugins/Events/");
                textbox1.IsReadOnly = false;
                textbox1.Text = "";
                var files = System.IO.Directory.EnumerateFiles("./Data/Plugins/Events/");
                if (files == null || files.Count() < 1) return;
                foreach (var file in files)
                    textbox1.Text += file.Split('/')[file.Split('/').Length - 1] + "\n";
            }
            catch { }
        }
        private async void LoadComboBox()
        {
            comboBox1.Items = null;
            events = await PluginManager.Online.ServerCom.ReadTextFromFile("https://sethdiscordbot.000webhostapp.com/Storage/Discord%20Bot/Plugins");
            if (events == null) return;
            string[] plugins = events.ToArray();
            string OS;
            var OSG = Functions.GetOperatingSystem();
            if (OSG == PluginManager.Others.OperatingSystem.WINDOWS) OS = "Windows";
            else if (OSG == PluginManager.Others.OperatingSystem.LINUX) OS = "Linux";
            else OS = "MAC_OS";
            List<string> data = new List<string>();
            for (int i = 0; i < plugins.Length; i++)
            {
                if (!plugins[i].Contains(OS) || !plugins[i].Contains("Event"))
                    continue;

                string[] info = plugins[i].Split(',');
                try
                {
                    if (System.IO.Directory.EnumerateFiles("./Data/Plugins/Events/").Any(x => x.EndsWith(info[0] + ".dll")))
                        continue;
                }
                catch { }


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
            string? URL = (from s in events
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

            await PluginManager.Online.ServerCom.DownloadFileAsync(URL, "./Data/Plugins/Events/" + pluginName + ".dll", progress);
            string? requirements = (from s in events
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

            label1.Content = "";

        }
    }
}
