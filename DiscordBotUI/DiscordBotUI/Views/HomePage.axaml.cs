using System;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;

using DiscordBotUI.Bot;

using DiscordBotCore;
using DiscordBotCore.Others.Logger;
using DiscordBotCore.Interfaces.Logger;

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
        await DiscordBotCore.Application.CreateApplication();

        if(!DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsAllKeys("token", "prefix"))
        {
            await new SettingsPage().ShowDialog(this);

            if (string.IsNullOrWhiteSpace(DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables["token"]) || string.IsNullOrWhiteSpace(DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables["prefix"]))
                Environment.Exit(-1);
        }

        
        textBoxToken.Text    = DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables["token"];
        textBoxPrefix.Text   = DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables["prefix"];
        textBoxServerId.Text = DiscordBotCore.Application.CurrentApplication.ApplicationEnvironmentVariables["ServerID"];
    }

    private void SetTextToTB(ILogMessage logMessage)
    {
        logTextBlock.Text += $"[{logMessage.LogMessageType}] [{logMessage.ThrowTime.ToShortTimeString()}] {logMessage.Message}\n";
    }

    private async void ButtonStartBotClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {

        DiscordBotCore.Application.CurrentApplication.Logger.OnRawLog += async (sender, logMessage) =>
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