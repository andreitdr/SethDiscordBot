using System;
using System.IO;
using System.Threading.Tasks;
using System.Net;
using System.Collections.Generic;

using PluginManager.Others;

namespace PluginManager.Online
{
    public class LanguageManager
    {
        private string link;

        /// <summary>
        /// The Language Manager constructor
        /// </summary>
        /// <param name="link">The link to where all the languages for the bot are stored</param>
        public LanguageManager(string link) => this.link = link;

        /// <summary>
        /// The method to list all languages
        /// </summary>
        /// <returns></returns>
        public async Task ListAllLanguages()
        {

            try
            {
                /*#pragma warning disable SYSLIB0014
                                WebClient client = new WebClient();
                #pragma warning restore SYSLIB0014
                                Stream data = await client.OpenReadTaskAsync(link);
                                string[] lines = (await new StreamReader(data).ReadToEndAsync()).Split('\n');*/
                List<string> list = await ServerCom.ReadTextFromFile(link);
                string[] lines = list.ToArray();

                List<string[]> info = new List<string[]>();
                info.Add(new string[] { "-", "-" });
                info.Add(new string[] { "Language Name", "File Size" });
                info.Add(new string[] { "-", "-" });
                foreach (var line in lines)
                {
                    if (line.Length <= 2) continue;
                    string[] d = line.Split(',');
                    if (d[3].Contains("cp") || d[3].Contains("CrossPlatform"))
                        info.Add(new string[] { d[0], d[1] });
                }
                info.Add(new string[] { "-", "-" });
                Console_Utilities.FormatAndAlignTable(info);
            }

            catch (Exception exception)
            {
                Console.WriteLine("Failed to execute command: listlang\nReason: " + exception.Message);
                Others.Functions.WriteErrFile(exception.ToString());
            }

        }

        /// <summary>
        /// A function that gets the download link for specified language
        /// </summary>
        /// <param name="langName">The name of the language</param>
        /// <returns></returns>
        public async Task<string[]?> GetDownloadLink(string langName)
        {
            try
            {
                List<string> list = await ServerCom.ReadTextFromFile(link);
                string[] lines = list.ToArray();

                foreach (var line in lines)
                {
                    if (line.Length <= 2) continue;
                    string[] d = line.Split(',');
                    if (d[0].Equals(langName) && (d[3].Contains("cp") || d[3].Contains("CrossPlatform")))
                        return new string[] { d[2], d[3] };
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Failed to execute command: listlang\nReason: " + exception.Message);
                Others.Functions.WriteErrFile(exception.ToString());
            }


            return null;
        }
    }
}