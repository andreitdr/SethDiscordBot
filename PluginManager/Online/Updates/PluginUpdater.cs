using PluginManager.Items;
using PluginManager.Online.Helpers;
using PluginManager.Others;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace PluginManager.Online.Updates
{
    public class PluginUpdater
    {
        public static async Task<bool> CheckForUpdates(string pakName)
        {
            try
            {
                var webV = await Online.ServerCom.GetVersionOfPackageFromWeb(pakName);
                var local = Online.ServerCom.GetVersionOfPackage(pakName);

                if (local is null) return true;
                if (webV is null) return false;

                if (webV == local) return false;
                if (webV > local) return true;
            }
            catch (Exception ex) { Console.WriteLine(ex.Message); }


            return false;
        }

        public static async Task<Update> DownloadUpdateInfo(string pakName)
        {
            string url = "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/Versions";
            List<string> info = await ServerCom.ReadTextFromURL(url);
            VersionString? version = await Online.ServerCom.GetVersionOfPackageFromWeb(pakName);

            if (version is null) return Update.Empty;
            Update update = new Update(pakName, string.Join('\n', info), version);
            return update;
        }

        public static async Task Download(string pakName)
        {
            Console_Utilities.WriteColorText("An update was found for &g" + pakName + "&c. Version: &r" + (await Online.ServerCom.GetVersionOfPackageFromWeb(pakName))?.ToShortString() + "&c. Current Version: &y" + Online.ServerCom.GetVersionOfPackage(pakName)?.ToShortString());
            await ConsoleCommandsHandler.ExecuteCommad("dwplug " + pakName);
        }


    }
}
