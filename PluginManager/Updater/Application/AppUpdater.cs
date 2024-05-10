using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

using PluginManager.Interfaces.Updater;

namespace PluginManager.Updater.Application
{
    public class AppUpdater
    {
        private static readonly string _DefaultUpdateUrl = "https://github.com/andreitdr/SethDiscordBot/releases/latest";
        
        private async Task<AppVersion> GetOnlineVersion()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(_DefaultUpdateUrl);

            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                var version = Regex.Match(content, @"<title>.+?v(\d+\.\d+\.\d+.\d+).+?</title>").Groups[1].Value;

                return new AppVersion(version);
            }

            return AppVersion.CurrentAppVersion;
        }

        public async Task<Update> CheckForUpdates()
        {
            var latestVersion = await GetOnlineVersion();
            if(latestVersion.IsNewerThan(AppVersion.CurrentAppVersion))
            {
                return new Update(AppVersion.CurrentAppVersion, latestVersion, _DefaultUpdateUrl, await GetUpdateNotes());
            }

            return Update.None;
        }

        private async Task<string> GetUpdateNotes()
        {
            HttpClient client = new HttpClient();
            var response = await client.GetAsync(_DefaultUpdateUrl);

            if (!response.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            var content = await response.Content.ReadAsStringAsync();
            var markdownStart = content.IndexOf("<div data-pjax=\"true\" data-test-selector=\"body-content\"");
            if(markdownStart == -1)
            {
                return string.Empty;
            }

            markdownStart = content.IndexOf(">", markdownStart) + 1; // Move past the opening tag
            var markdownEnd = content.IndexOf("</div>", markdownStart);
            var markdown = content.Substring(markdownStart, markdownEnd - markdownStart).Trim();
            markdown = RemoveHtmlTags(markdown);

            markdown = ApplyMarkdownFormatting(markdown);

            return markdown;

        }

        private string RemoveHtmlTags(string text)
        {
            return Regex.Replace(text, "<.*?>", "").Trim();
        }
        private string ApplyMarkdownFormatting(string markdown)
        {
            // Apply markdown formatting
            markdown = markdown.Replace("**", "**"); // Bold
            markdown = markdown.Replace("*", "*"); // Italic
            markdown = markdown.Replace("`", "`"); // Inline code
            markdown = markdown.Replace("```", "```"); // Code block
            markdown = markdown.Replace("&gt;", ">"); // Greater than symbol
            markdown = markdown.Replace("&lt;", "<"); // Less than symbol
            markdown = markdown.Replace("&amp;", "&"); // Ampersand
            markdown = markdown.Replace("&quot;", "\""); // Double quote
            markdown = markdown.Replace("&apos;", "'"); // Single quote
            markdown = markdown.Replace(" - ", "\n- "); // Convert bullet points to markdown list items

            return markdown;
        }

    }
}
