using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PluginManager.Others;

namespace DiscordBotGUI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop) { desktop.MainWindow = new AppUpdater() { Width = 300, Height = 50, WindowStartupLocation = Avalonia.Controls.WindowStartupLocation.CenterScreen }; }

            base.OnFrameworkInitializationCompleted();
        }
    }
}
