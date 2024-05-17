using Avalonia.Controls;

using DiscordBotCore;

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

        DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables.Add("token", token);
        DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables.Add("prefix", botPrefix);
        DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables.Add("ServerID", serverId);

        await DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables.SaveToFile();

        DiscordBotCore.Application.CurrentApplication.Logger.Log("Config Saved", this);

        Close();

    }
}