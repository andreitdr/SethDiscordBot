using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using DiscordBotGUI.Settings;
using PluginManager;
using PluginManager.Others;

namespace DiscordBotGUI;

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
        if (File.Exists("./Version.txt")) label5.Content = Config.GetValue<string>("Version");
        button1.Click += async (sender, e) =>
        {
            var token  = textBox1.Text;
            var prefix = textBox2.Text;
            var args   = "--nomessage " + textBox3.Text;

            if (!((token.Length == 70 || token.Length == 59) && prefix.Length == 1))
            {
                label4.Content = "Invalid Token or Prefix.\n(Prefix must be 1 character long and token must be 59 or 79 characters long)";
                await Task.Delay(5000);
                label4.Content = "";
                return;
            }

            Config.SetValue("token", token);
            Config.SetValue("prefix", prefix);
            RunDiscordBot(args);
        };

        commandsSettingMenuItem.Click      += (sender, e) => new Commands() /*{ Height = 200, Width = 550 }*/.ShowDialog(this);
        eventsSettingMenuItem.Click        += (sender, e) => new Events() /*{ Height = 200, Width = 550 }*/.ShowDialog(this);
        applicationVariablesMenuItem.Click += (sender, e) => new ApplicationVariables().ShowDialog(this);

        var folder = $"{Functions.dataFolder}DiscordBotCore.data";
        Directory.CreateDirectory(Functions.dataFolder);
        try
        {
            var botToken  = Config.GetValue<string>("token");
            var botPrefix = Config.GetValue<string>("prefix");
            if (botToken == null || botPrefix == null)
            {
                textBox1.IsReadOnly = false;
                textBox2.IsReadOnly = false;
                textBox1.Watermark  = "Insert Bot Token Here";
                textBox2.Watermark  = "Insert Bot Prefix Here";
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
            textBox1.Watermark  = "Insert Bot Token Here";
            textBox2.Watermark  = "Insert Bot Prefix Here";
        }
    }


    private void RunDiscordBot(string args)
    {
        var os = Functions.GetOperatingSystem();
        if (os == OperatingSystem.WINDOWS)
            Process.Start("./DiscordBot.exe", args);
        else if (os == OperatingSystem.LINUX)
            Process.Start("./DiscordBot", args);
        else if (os == OperatingSystem.MAC_OS) Process.Start("./DiscordBot.app/Contents/MacOS/DiscordBot", args);
        Close();
    }
}
