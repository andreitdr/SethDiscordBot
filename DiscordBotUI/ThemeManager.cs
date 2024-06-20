using DiscordBotUI_Windows.Theming;

namespace DiscordBotUI_Windows
{
    internal class ThemeManager
    {
        private readonly string _ThemesFolder = "DiscordBotUI/Themes";
        private readonly Theme _DefaultTheme = new Theme
        {
            Name = "Default",
            Description = "Default theme",
            Type = ThemeType.Undefined,
            ThemeValues = new Dictionary<string, string>
            {
                { "BackgroundColor", "#36393F" },
                { "TextColor", "#000000" }
            }
        };

        internal Theme CurrentTheme { get; private set; }

        internal IList<Theme> _InstalledThemes { get; private set; }

        internal ThemeManager()
        {
            _InstalledThemes = new List<Theme>();
            CurrentTheme = _DefaultTheme;
        }

        private void SetControlTheme(Control control, Theme theme, bool doChildToo)
        {
            
            control.BackColor = ColorTranslator.FromHtml(theme.ThemeValues["BackgroundColor"]);
            control.ForeColor = ColorTranslator.FromHtml(theme.ThemeValues["TextColor"]);

            // Fix for data grid view
            if (control is DataGridView dataGridView)
            {
                dataGridView.BackgroundColor = control.BackColor;
                dataGridView.DefaultCellStyle.BackColor = control.BackColor;
                dataGridView.DefaultCellStyle.ForeColor = control.ForeColor;
                dataGridView.DefaultCellStyle.SelectionBackColor = control.BackColor;
                dataGridView.DefaultCellStyle.SelectionForeColor = control.ForeColor;

                dataGridView.ColumnHeadersDefaultCellStyle.BackColor = control.BackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.ForeColor = control.ForeColor;
                dataGridView.ColumnHeadersDefaultCellStyle.SelectionBackColor = control.BackColor;
                dataGridView.ColumnHeadersDefaultCellStyle.SelectionForeColor = control.ForeColor;

            }



            if (doChildToo)
            {
                foreach (Control childControl in control.Controls)
                {
                    SetControlTheme(childControl, theme, true);
                }
            }
        }

        internal void SetFormTheme(Theme theme, Form windowsForm)
        {
            CurrentTheme = theme;
            SetControlTheme(windowsForm, theme, true);
            Config.ApplicationSettings["AppTheme"] = theme.Name;
        }

        internal void SetTheme(string themeName)
        {
            CurrentTheme = _InstalledThemes.FirstOrDefault(x => x.Name == themeName, _DefaultTheme);
            Config.ApplicationSettings["AppTheme"] = themeName;
            
        }

        private async Task<Theme> LoadThemeFromFile(string fileName)
        {
            return await DiscordBotCore.Others.JsonManager.ConvertFromJson<Theme>(fileName);
        }

        internal async Task SaveThemeToFile(string themeName)
        {
            Theme? theme = _InstalledThemes.FirstOrDefault(x => x.Name == themeName);
            if (theme == null)
                throw new Exception("Theme not found");

            string basefolderPath = DiscordBotCore.Application.GetResourceFullPath(_ThemesFolder);
            Directory.CreateDirectory(basefolderPath);
            string filePath = Path.Combine(basefolderPath, $"{themeName}.json");
            await DiscordBotCore.Others.JsonManager.SaveToJsonFile(filePath, theme);
        }

        internal async Task<int> LoadThemesFromThemesFolder()
        {
            string basefolderPath = DiscordBotCore.Application.GetResourceFullPath(_ThemesFolder);
            Directory.CreateDirectory(basefolderPath);
            var files = Directory.GetFiles(basefolderPath, "*.json");
            _InstalledThemes.Clear();
            if(files.Length == 0)
            {
                _InstalledThemes.Add(_DefaultTheme);
                return 1;
            }

            foreach (var file in files)
            {
                var theme = await LoadThemeFromFile(file);
                _InstalledThemes.Add(theme);
            }

            return _InstalledThemes.Count;
        }
    }
}
