using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;

using PluginManager.Others;

namespace PluginManager.Online
{
    public class PluginsManager
    {
        /// <summary>
        /// The URL of the server
        /// </summary>
        public string PluginsLink { get; private set; }

        /// <summary>
        /// The Plugin Manager constructor
        /// </summary>
        /// <param name="link">The link to the file where all plugins are stored</param>
        public PluginsManager(string link)
        {
            PluginsLink = link;
        }

        /// <summary>
        /// The method to load all plugins
        /// </summary>
        /// <returns></returns>
        public async Task ListAvailablePlugins()
        {
            try
            {
                List<string> list = await ServerCom.ReadTextFromFile(PluginsLink);
                string[] lines = list.ToArray();

                List<string[]> data = new List<string[]>();
                var op = Functions.GetOperatinSystem();

                int len = lines.Length;
                string[] titles = { "Name", "Description", "Plugin Type", "Libraries" };
                data.Add(new string[] { "-", "-", "-", "-" });
                data.Add(titles);
                data.Add(new string[] { "-", "-", "-", "-" });
                for (int i = 0; i < len; i++)
                {
                    if (lines[i].Length <= 2) continue;
                    string[] content = lines[i].Split(',');
                    string[] display = new string[4];
                    if (op == Others.OperatingSystem.WINDOWS)
                    {
                        if (content[4].Contains("Windows"))
                        {
                            display[0] = content[0];
                            display[1] = content[1];
                            display[2] = content[2];
                            if (content.Length == 6 && (content[5] != null || content[5].Length > 2))
                                display[3] = ((await ServerCom.ReadTextFromFile(content[5])).Count + 1).ToString();

                            else display[3] = "1";
                            data.Add(display);
                            continue;
                        }
                    }
                    else if (op == Others.OperatingSystem.LINUX)
                    {
                        if (content[4].Contains("Linux"))
                        {
                            display[0] = content[0];
                            display[1] = content[1];
                            display[2] = content[2];
                            data.Add(display);
                            continue;
                        }
                    }
                }

                data.Add(new string[] { "-", "-", "-", "-" });

                Console_Utilities.FormatAndAlignTable(data);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to execute command: listlang\nReason: " + exception.Message);
                Others.Functions.WriteErrFile(exception.ToString());
            }

        }

        /// <summary>
        /// The method to get plugin information by its name
        /// </summary>
        /// <param name="name">The plugin name</param>
        /// <returns></returns>
        public async Task<string[]> GetPluginLinkByName(string name)
        {
            try
            {
                List<string> list = await ServerCom.ReadTextFromFile(PluginsLink);
                string[] lines = list.ToArray();
                int len = lines.Length;
                for (int i = 0; i < len; i++)
                {
                    string[] contents = lines[i].Split(',');
                    if (contents[0] == name)
                    {
                        if (contents.Length == 6)
                            return new string[] { contents[2], contents[3], contents[5] };
                        else if (contents.Length == 5)
                            return new string[] { contents[2], contents[3], string.Empty };
                        else throw new Exception("Failed to download plugin. Invalid Argument Length");
                    }

                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to execute command: listplugs\nReason: " + exception.Message);
                Others.Functions.WriteErrFile(exception.ToString());
            }

            return new string[] { null!, null!, null! };
        }


    }
}
