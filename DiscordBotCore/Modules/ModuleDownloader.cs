using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using DiscordBotCore.Online;

namespace DiscordBotCore.Modules
{
    public class ModuleDownloader
    {
        private string _moduleName;
        private readonly string _baseUrl = "https://raw.githubusercontent.com/andreitdr/SethPlugins/tests/Modules/";
        private readonly string _moduleFolder = "./Data/Modules";

        public ModuleDownloader(string moduleName)
        {
            _moduleName = moduleName;
        }

        public async Task DownloadModule(IProgress<float> progressToWrite)
        {
            Directory.CreateDirectory(_moduleFolder);
            string url = _baseUrl + _moduleName + ".dll";
            await ServerCom.DownloadFileAsync(url, _moduleFolder + "/" + _moduleName + ".dll", progressToWrite);
        }
    }
}
