using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginManager.Others;

namespace PluginManager.Online;

public class LanguageManager
{
    private readonly string link;

    /// <summary>
    ///     The Language Manager constructor
    /// </summary>
    /// <param name="link">The link to where all the languages for the bot are stored</param>
    public LanguageManager(string link)
    {
        this.link = link;
    }

    /// <summary>
    ///     The method to list all languages
    /// </summary>
    /// <returns></returns>
    public async Task ListAllLanguages()
    {
        try
        {
            var list  = await ServerCom.ReadTextFromFile(link);
            var lines = list.ToArray();

            var info = new List<string[]>();
            info.Add(new[] { "-", "-" });
            info.Add(new[] { "Language Name", "File Size" });
            info.Add(new[] { "-", "-" });
            foreach (var line in lines)
            {
                if (line.Length <= 2) continue;
                var d = line.Split(',');
                if (d[3].Contains("cp") || d[3].Contains("CrossPlatform")) info.Add(new[] { d[0], d[1] });
            }

            info.Add(new[] { "-", "-" });
            Console_Utilities.FormatAndAlignTable(info);
        }

        catch (Exception exception)
        {
            Console.WriteLine("Failed to execute command: listlang\nReason: " + exception.Message);
            Functions.WriteErrFile(exception.ToString());
        }
    }

    /// <summary>
    ///     A function that gets the download link for specified language
    /// </summary>
    /// <param name="langName">The name of the language</param>
    /// <returns></returns>
    public async Task<string[]?> GetDownloadLink(string langName)
    {
        try
        {
            var list  = await ServerCom.ReadTextFromFile(link);
            var lines = list.ToArray();

            foreach (var line in lines)
            {
                if (line.Length <= 2) continue;
                var d = line.Split(',');
                if (d[0].Equals(langName) && (d[3].Contains("cp") || d[3].Contains("CrossPlatform"))) return new[] { d[2], d[3] };
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("Failed to execute command: listlang\nReason: " + exception.Message);
            Functions.WriteErrFile(exception.ToString());
        }


        return null;
    }
}
