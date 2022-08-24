using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PluginManager.Others;

using OperatingSystem = PluginManager.Others.OperatingSystem;

namespace PluginManager.Online;

public class PluginsManager
{
    /// <summary>
    ///     The Plugin Manager constructor
    /// </summary>
    /// <param name="link">The link to the file where all plugins are stored</param>
    public PluginsManager(string link)
    {
        PluginsLink = link;
    }

    /// <summary>
    ///     The URL of the server
    /// </summary>
    public string PluginsLink { get; }

    /// <summary>
    ///     The method to load all plugins
    /// </summary>
    /// <returns></returns>
    public async Task ListAvailablePlugins()
    {
        try
        {
            var list = await ServerCom.ReadTextFromURL(PluginsLink);
            var lines = list.ToArray();

            var data = new List<string[]>();
            var op = Functions.GetOperatingSystem();

            var len = lines.Length;
            string[] titles = { "Name", "Description", "Plugin Type", "Libraries", "Installed" };
            data.Add(new[] { "-", "-", "-", "-", "-" });
            data.Add(titles);
            data.Add(new[] { "-", "-", "-", "-", "-" });
            for (var i = 0; i < len; i++)
            {
                if (lines[i].Length <= 2)
                    continue;
                var content = lines[i].Split(',');
                var display = new string[titles.Length];
                if (op == OperatingSystem.WINDOWS)
                {
                    if (content[4].Contains("Windows"))
                    {
                        display[0] = content[0];
                        display[1] = content[1];
                        display[2] = content[2];
                        if (content.Length == 6 && (content[5] != null || content[5].Length > 2))
                            display[3] = ((await ServerCom.ReadTextFromURL(content[5])).Count + 1).ToString();

                        else
                            display[3] = "1";
                        if (Config.PluginConfig.Contains(content[0]) || Config.PluginConfig.Contains(content[0]))
                            display[4] = "✓";
                        else
                            display[4] = "X";
                        data.Add(display);
                    }
                }
                else if (op == OperatingSystem.LINUX)
                {
                    if (content[4].Contains("Linux"))
                    {
                        display[0] = content[0];
                        display[1] = content[1];
                        display[2] = content[2];
                        if (content.Length == 6 && (content[5] != null || content[5].Length > 2))
                            display[3] = ((await ServerCom.ReadTextFromURL(content[5])).Count + 1).ToString();
                        if (Config.PluginConfig.Contains(content[0]) || Config.PluginConfig.Contains(content[0]))
                            display[4] = "✓";
                        else
                            display[4] = "X";
                        data.Add(display);
                    }
                }
            }

            data.Add(new[] { "-", "-", "-", "-", "-" });

            Console_Utilities.FormatAndAlignTable(data, TableFormat.CENTER_EACH_COLUMN_BASED);
        }
        catch (Exception exception)
        {
            Console.WriteLine("Failed to execute command: listplugs\nReason: " + exception.Message);
            Functions.WriteErrFile(exception.ToString());
        }
    }

    /// <summary>
    ///     The method to get plugin information by its name
    /// </summary>
    /// <param name="name">The plugin name</param>
    /// <returns></returns>
    public async Task<string[]> GetPluginLinkByName(string name)
    {
        try
        {
            var list = await ServerCom.ReadTextFromURL(PluginsLink);
            var lines = list.ToArray();
            var len = lines.Length;
            for (var i = 0; i < len; i++)
            {
                var contents = lines[i].Split(',');
                if (contents[0] == name)
                {
                    if (contents.Length == 6)
                        return new[] { contents[2], contents[3], contents[5] };
                    if (contents.Length == 5)
                        return new[] { contents[2], contents[3], string.Empty };
                    throw new Exception("Failed to download plugin. Invalid Argument Length");
                }
            }
        }
        catch (Exception exception)
        {
            Console.WriteLine("Failed to execute command: listplugs\nReason: " + exception.Message);
            Functions.WriteErrFile(exception.ToString());
        }

        return new string[] { null!, null!, null! };
    }
}
