using System;
using System.IO;
using System.Threading.Tasks;

using DiscordBotCore.Online;

namespace DiscordBotCore.Modules
{
    public class ModuleDownloader
    {
        private readonly string _ModuleName;
        private const    string _BaseUrl      = "https://raw.githubusercontent.com/andreitdr/SethPlugins/tests/Modules/";

        public ModuleDownloader(string moduleName)
        {
            _ModuleName = moduleName;
        }

        public async Task DownloadModule(IProgress<float> progressToWrite)
        {
            string? moduleFolder = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("ModuleFolder");
            
            if(moduleFolder is null)
                throw new DirectoryNotFoundException("Module folder not found"); // Should never happen
            
            Directory.CreateDirectory(moduleFolder);
            string url = _BaseUrl + _ModuleName + ".dll";
            await ServerCom.DownloadFileAsync(url, moduleFolder + "/" + _ModuleName + ".dll", progressToWrite);
        }
    }
}
