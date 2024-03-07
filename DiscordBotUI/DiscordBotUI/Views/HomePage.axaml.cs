using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using DiscordBotUI.Bot;

using PluginManager;
using PluginManager.Others.Logger;

namespace DiscordBotUI.Views;

public partial class HomePage : Window
{
    private readonly DiscordBot _DiscordBot;

    public HomePage()
    {
        InitializeComponent();
        _DiscordBot = new DiscordBot(null!);

        Loaded += HomePage_Loaded;
    }

    private async void HomePage_Loaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await Config.Initialize();
        
        while(string.IsNullOrWhiteSpace(Config.AppSettings["token"]) || string.IsNullOrWhiteSpace(Config.AppSettings["prefix"]))
        {
            await new SettingsPage().ShowDialog(this);
        }
        
        textBoxToken.Text    = Config.AppSettings["token"];
        textBoxPrefix.Text   = Config.AppSettings["prefix"];
        textBoxServerId.Text = Config.AppSettings["ServerID"];
    }

    private void SetTextToTB(Log logMessage)
    {
        logTextBlock.Text += $"[{logMessage.Type}] [{logMessage.ThrowTime.ToShortTimeString()}] {logMessage.Message}\n";
    }

    private async void ButtonStartBotClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

        Config.Logger.OnLog += async (sender, logMessage) =>
        {
            await Dispatcher.UIThread.InvokeAsync(() => SetTextToTB(logMessage), DispatcherPriority.Background);
        };

        await _DiscordBot.InitializeBot();

        await _DiscordBot.LoadPlugins();
        
    }

    private async void SettingsMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //await new SettingsPage().ShowDialog(this);
        new SettingsPage().Show();
    }

    private async void PluginsMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        //await new PluginsPage().ShowDialog(this);
        new PluginsPage().Show();
    }

    private void NewPluginsMenuClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        new PluginInstaller().Show();
    }
}