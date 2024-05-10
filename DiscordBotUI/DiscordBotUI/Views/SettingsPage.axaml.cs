using Avalonia.Controls;

using PluginManager;

namespace DiscordBotUI.Views;

public partial class SettingsPage : Window
{
    public SettingsPage()
    {
        InitializeComponent();
    }

    private async void ButtonSaveSettingsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        string token = textBoxToken.Text;
        string botPrefix = textBoxPrefix.Text;
        string serverId = textBoxServerId.Text;

        if (string.IsNullOrWhiteSpace(serverId)) serverId = string.Empty;
        if (string.IsNullOrWhiteSpace(token))
        {
            labelErrorMessage.Content = "The token is invalid";
            return;
        }

        if(string.IsNullOrWhiteSpace(botPrefix) || botPrefix.Length > 1 || botPrefix.Length < 1)
        {
            labelErrorMessage.Content = "The prefix is invalid";
            return;
        }

        Config.AppSettings.Add("token", token);
        Config.AppSettings.Add("prefix", botPrefix);
        Config.AppSettings.Add("ServerID", serverId);

        await Config.AppSettings.SaveToFile();

        Config.Logger.Log("Config Saved");

        Close();

    }
}