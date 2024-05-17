using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;

using DiscordBotUI.ViewModels;

using DiscordBotCore;
using DiscordBotCore.Plugin;

namespace DiscordBotUI.Views;

public partial class PluginInstaller : Window
{

    public ObservableCollection<OnlinePlugin> Plugins { get; private set; }

    public PluginInstaller()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;

    }

    private async void OnPageLoaded(object? sender, RoutedEventArgs e)
    {
        if (DiscordBotCore.Application.CurrentApplication.PluginManager is null) return;

        List<PluginOnlineInfo>? onlineInfos = await DiscordBotCore.Application.CurrentApplication.PluginManager.GetPluginsList();

        if(onlineInfos is null) return;

        List<OnlinePlugin> plugins = new List<OnlinePlugin>();

        foreach(PluginOnlineInfo onlinePlugin in onlineInfos)
        {
            plugins.Add(new OnlinePlugin(onlinePlugin.Name, onlinePlugin.Description, onlinePlugin.Version.ToShortString()));
        }

        Plugins = new ObservableCollection<OnlinePlugin>(plugins);

        dataGridInstallablePlugins.ItemsSource = Plugins;

    }

    public async void InstallPlugin(string name)
    {
        
        PluginOnlineInfo? info = await DiscordBotCore.Application.CurrentApplication.PluginManager.GetPluginDataByName(name);
        if(info is null) return;

        

        await DiscordBotCore.Application.CurrentApplication.PluginManager.InstallPlugin(info, null);
    }
}