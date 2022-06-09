using Avalonia.Controls;
using PluginManager;

namespace DiscordBotGUI.Settings;

public partial class ApplicationVariables : Window
{
    public ApplicationVariables()
    {
        InitializeComponent();
        Load();
    }

    private void Load()
    {
        ClearEverything();
        button1.Click += (sedner, e) =>
        {
            var key = textBox2.Text;
            if (Config.ContainsKey(key))
            {
                ClearEverything();
                return;
            }

            var value = textBox3.Text;
            Config.AddValueToVariables(key, value, checkBox1.IsChecked!.Value);
            ClearEverything();
        };
    }

    private void ClearEverything()
    {
        textBox1.Text       = "";
        textBox2.Text       = "";
        textBox3.Text       = "";
        checkBox1.IsChecked = false;
        var allvars                                = Config.GetAllVariables();
        foreach (var kvp in allvars) textBox1.Text += kvp.Key + " => " + kvp.Value + "\n";
    }
}
