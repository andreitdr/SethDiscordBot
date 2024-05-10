using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Interactivity;

using DiscordBotUI.ViewModels;

using PluginManager;

namespace DiscordBotUI.Views;


public partial class PluginsPage: Window
{

    public ObservableCollection<Plugin> Plugins { get; private set; } 

    public PluginsPage()
    {
        InitializeComponent();
        Loaded += OnPageLoaded;
    }

    private async void OnPageLoaded(object? sender, RoutedEventArgs e)
    {
        if (Config.PluginsManager is null) return;

        var plugins = await Config.PluginsManager.GetInstalledPlugins();
        var localList = new List<Plugin>();
        foreach (var plugin in plugins)
        {
            localList.Add(new Plugin(plugin.PluginName, plugin.PluginVersion.ToShortString(), plugin.IsMarkedToUninstall));
        }

        Plugins = new ObservableCollection<Plugin>(localList);


        dataGridPlugins.ItemsSource = Plugins;
    }


}