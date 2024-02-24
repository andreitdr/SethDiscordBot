using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PluginManager.Online.Helpers;
using PluginManager.Others;
namespace PluginManager.Online;

public class PluginsManager
{
    /// <summary>
    /// The Plugin Manager constructor. It uses the default links and the default branch.
    /// </summary>
    /// <param name="branch">The main branch from where the plugin manager gets its info</param>
    public PluginsManager(string? branch)
    {
        PluginsLink  = $"https://raw.githubusercontent.com/andreitdr/SethPlugins/{branch}/PluginsList";
        VersionsLink = $"https://raw.githubusercontent.com/andreitdr/SethPlugins/{branch}/Versions";
    }

    /// <summary>
    ///     The URL of the server
    /// </summary>
    public string PluginsLink { get; }

    public string VersionsLink { get; }

    /// <summary>
    ///     The method to load all plugins
    /// </summary>
    /// <returns></returns>
    public async Task<List<string[]>> GetAvailablePlugins()
    {
        // Config.Logger.Log("Got data from " + VersionsLink, this, LogLevel.INFO);
        try
        {
            var list  = await ServerCom.ReadTextFromURL(PluginsLink);
            var lines = list.ToArray();

            var data = new List<string[]>();

            var len = lines.Length;
            for (var i = 0; i < len; i++)
            {
                if (lines[i].Length <= 2)
                    continue;
                var content = lines[i].Split(',');
                var display = new string[4]; // 4 columns
                if (System.OperatingSystem.IsWindows())
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
                else if (System.OperatingSystem.IsLinux())
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
            return data;
        }
        catch (Exception exception)
        {
            Config.Logger.Log(message: "Failed to execute command: listplugs\nReason: " + exception.Message, source: typeof(PluginsManager), type: LogType.ERROR);
        }

        return null;
    }

    private async Task<VersionString?> GetVersionOfPackageFromWeb(string pakName)
    {
        var data = await ServerCom.ReadTextFromURL(VersionsLink);
        foreach (var item in data)
        {
            if (item.StartsWith("#"))
                continue;

            var split = item.Split(',');
            if (split[0] == pakName)
            {
                // Config.Logger.Log("Searched for " + pakName + " and found " + split[1] + " as version.", LogLevel.INFO);
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
            var list  = await ServerCom.ReadTextFromURL(PluginsLink);
            var lines = list.ToArray();
            var len   = lines.Length;
            for (var i = 0; i < len; i++)
            {
                var contents = lines[i].Split(',');
                if (contents[0].ToLowerInvariant() == name.ToLowerInvariant())
                {
                    if (System.OperatingSystem.IsWindows() && contents[4].Contains("Windows"))
                    {
                        if (contents.Length == 6)
                            return new[]
                            {
                                contents[2], contents[3], contents[5]
                            };
                        if (contents.Length == 5)
                            return new[]
                            {
                                contents[2], contents[3], string.Empty
                            };
                        throw new Exception("Failed to download plugin. Invalid Argument Length");

                    }

                    if (System.OperatingSystem.IsLinux() && contents[4].Contains("Linux"))
                    {
                        if (contents.Length == 6)
                            return new[]
                            {
                                contents[2], contents[3], contents[5]
                            };
                        if (contents.Length == 5)
                            return new[]
                            {
                                contents[2], contents[3], string.Empty
                            };
                        throw new Exception("Failed to download plugin. Invalid Argument Length");

                    }
                }

            }
        }
        catch (Exception exception)
        {
            Config.Logger.Log("Failed to execute command: plugin list\nReason: " + exception.Message, source: typeof(PluginsManager), type: LogType.ERROR);
        }

        return null;
    }
}
