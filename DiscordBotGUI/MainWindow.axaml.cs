using Avalonia.Controls;

using System.Threading.Tasks;

using PluginManager.Others;
using System.IO;
using System;
using System.Diagnostics;
using DiscordBotGUI.Settings;

namespace DiscordBotGUI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            LoadElements();
        }

        private void LoadElements()
        {
            textBox3.Watermark = "Insert start arguments";
            button1.Click += async (sender, e) =>
            {

                string token = textBox1.Text;
                string prefix = textBox2.Text;
                string args = textBox3.Text;

                if (!((token.Length == 70 || token.Length == 59) && prefix.Length == 1))
                {
                    label4.Content = "Invalid Token or Prefix.\n(Prefix must be 1 character long and token must be 59 or 79 characters long)";
                    await Task.Delay(5000);
                    label4.Content = "";
                    return;
                }

                Functions.WriteToSettings(Functions.dataFolder + "DiscordBotCore.data", "BOT_TOKEN", token, '=');
                Functions.WriteToSettings(Functions.dataFolder + "DiscordBotCore.data", "BOT_PREFIX", prefix, '=');
                RunDiscordBot(args);

            };

            commandsSettingMenuItem.Click += (sender, e) => new Commands().ShowDialog(this);
            eventsSettingMenuItem.Click += (sender, e) => new Events().ShowDialog(this);

            string folder = $"{Functions.dataFolder}DiscordBotCore.data";
            Directory.CreateDirectory(Functions.dataFolder);
            try
            {
                string? botToken = Functions.readCodeFromFile(folder, "BOT_TOKEN", '=');
                string? botPrefix = Functions.readCodeFromFile(folder, "BOT_PREFIX", '=');
                if (botToken == null || botPrefix == null)
                {
                    textBox1.IsReadOnly = false;
                    textBox2.IsReadOnly = false;
                    textBox1.Watermark = "Insert Bot Token Here";
                    textBox2.Watermark = "Insert Bot Prefix Here";

                }
                else
                {
                    textBox1.Text = botToken;
                    textBox2.Text = botPrefix;
                }
            }
            catch
            {
                textBox1.IsReadOnly = false;
                textBox2.IsReadOnly = false;
                textBox1.Watermark = "Insert Bot Token Here";
                textBox2.Watermark = "Insert Bot Prefix Here";
            }

        }


        private void RunDiscordBot(string args)
        {
            var os = Functions.GetOperatingSystem();
            if (os == PluginManager.Others.OperatingSystem.WINDOWS)
                Process.Start("./DiscordBot.exe", args);
            else if (os == PluginManager.Others.OperatingSystem.LINUX)
                Process.Start("./DiscordBot", args);
            else if (os == PluginManager.Others.OperatingSystem.MAC_OS)
                Process.Start("./DiscordBot.app/Contents/MacOS/DiscordBot", args);
            Close();
            return;
        }
    }
}
