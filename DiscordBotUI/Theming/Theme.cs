using System.Text.Json.Serialization;

namespace DiscordBotUI_Windows.Theming
{
    internal class Theme 
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public ThemeType Type { get; set; }
        public IDictionary<string, string> ThemeValues { get; set; }

        internal void SetThemeValue(string key, string value)
        {
            if (ThemeValues.ContainsKey(key))
            {
                ThemeValues[key] = value;
            }
        }
    }
}
