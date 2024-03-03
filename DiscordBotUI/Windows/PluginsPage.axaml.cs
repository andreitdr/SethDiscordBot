using System.Collections.Generic;
using System.Collections.ObjectModel;

using Avalonia.Controls;
using Avalonia.Interactivity;
using PluginManager;

namespace DiscordBotUI.Windows;


public partial class PluginsPage: Window
{
    public ObservableCollection<PluginViewModel> _Plugins { get; private set; }
    public PluginsPage()
    {
        InitializeComponent();
        
        Loaded += OnAppLoaded;
    }

    private async void OnAppLoaded(object? sender, RoutedEventArgs e)
    {
        var plugins = await Config.PluginsManager.GetInstalledPlugins();
        _Plugins = new ObservableCollection<PluginViewModel>();
        foreach (var plugin in plugins)
        {
            _Plugins.Add(new PluginViewModel(plugin.PluginName, plugin.PluginVersion.ToShortString(), plugin.IsMarkedToUninstall));
        }
        
        dataGridPlugins.ItemsSource = _Plugins;
    }


}

public class PluginViewModel
{
    public string Name { get; set; }
    public string Version { get; set; }
    public bool IsMarkedToUninstall { get; set; }

    public PluginViewModel(string Name, string Version, bool isMarkedToUninstall)
    {
        this.Name = Name;
        this.Version = Version;
        IsMarkedToUninstall = isMarkedToUninstall;
    }
}