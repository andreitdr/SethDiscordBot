using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using PluginManager.Online.Helpers;
using PluginManager.Others;

using OperatingSystem = PluginManager.Others.OperatingSystem;

namespace PluginManager.Online;

public class PluginsManager
{
    /// <summary>
    ///     The Plugin Manager constructor
    /// </summary>
    /// <param name="plink">The link to the file where all plugins are stored</param>
    /// <param name="vlink">The link to the file where all plugin versions are stored</param>
    public PluginsManager(string plink, string vlink)
    {
        PluginsLink = plink;
        VersionsLink = vlink;
    }

    /// <summary>
    ///     The URL of the server
    /// </summary>
    public string PluginsLink { get; }
    public string VersionsLink {get; }

    /// <summary>
    ///     The method to load all plugins
    /// </summary>
    /// <returns></returns>
    public async Task<List<string[]>> GetAvailablePlugins()
    {
        try
        {
            var list = await ServerCom.ReadTextFromURL(PluginsLink);
            var lines = list.ToArray();

            var data = new List<string[]>();
            var op = Functions.GetOperatingSystem();

            var len = lines.Length;
            for (var i = 0; i < len; i++)
            {
                if (lines[i].Length <= 2)
                    continue;
                var content = lines[i].Split(',');
                var display = new string[4]; // 4 columns
                if (op == OperatingSystem.WINDOWS)
                {
                    if (content[4].Contains("Windows"))
                    {
                        display[0] = content[0];
                        display[1] = content[1];
                        display[2] = content[2];
                        display[3] =
                            (await GetVersionOfPackageFromWeb(content[0]) ?? new VersionString("0.0.0"))
                           .ToShortString();
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
                        display[3] =
                            (await GetVersionOfPackageFromWeb(content[0]) ?? new VersionString("0.0.0"))
                           .ToShortString();
                        data.Add(display);
                    }
                }
            }

            data.Add(new[] { "-", "-", "-", "-" });

            return data;
        }
        catch (Exception exception)
        {
            Config.Logger.Log("Failed to execute command: listplugs\nReason: " + exception.Message, this, LogLevel.ERROR);
        }

        return null;
    }

    public async Task<VersionString?> GetVersionOfPackageFromWeb(string pakName)
    {
        var data = await ServerCom.ReadTextFromURL(VersionsLink);
        foreach (var item in data)
        {
            if (item.StartsWith("#"))
                continue;

            string[] split = item.Split(',');
            if (split[0] == pakName)
            {
                Console.WriteLine("Searched for " + pakName + " and found " + split[1] + " as version.\nUsed url: " + VersionsLink);
                return new VersionString(split[1]);
            }
                
        }
        return null;
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
            Config.Logger.Log("Failed to execute command: listplugs\nReason: " + exception.Message, this, LogLevel.ERROR);
        }

        return new string[] { null!, null!, null! };
    }
}