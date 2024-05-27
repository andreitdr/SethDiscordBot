namespace DiscordBotUI_Windows.WindowsForms
{
    public partial class MainWindow : Form
    {
        internal MainWindow()
        {
            InitializeComponent();
            Load += (_, _) => MainWindowLoad();
            FormClosed += async (_, _) =>
            {
                await Config.ApplicationSettings.SaveToFile();
            };
        }

        private void MainWindowLoad()
        {
            pluginListToolStripMenuItem.Click += (_, _) =>
            {
                var form = new PluginListWindow();
                Config.ThemeManager.SetFormTheme(Config.ThemeManager.CurrentTheme, form);
                form.Show();
            };
            themesToolStripMenuItem.Click += (_, _) => {
                themesToolStripMenuItem.DropDownItems.Clear();
                foreach(var theme in Config.ThemeManager._InstalledThemes)
                {
                    themesToolStripMenuItem.DropDownItems.Add(theme.Name, null, (_, _) => Config.ThemeManager.SetFormTheme(theme, this));
                }
            };
        }
    }
}
