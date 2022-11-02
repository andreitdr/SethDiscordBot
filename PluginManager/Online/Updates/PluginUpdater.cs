using System;
using System.Threading.Tasks;
using PluginManager.Items;
using PluginManager.Others;

namespace PluginManager.Online.Updates;

public class PluginUpdater
{
    public static async Task<bool> CheckForUpdates(string pakName)
    {
        try
        {
            var webV  = await ServerCom.GetVersionOfPackageFromWeb(pakName);
            var local = ServerCom.GetVersionOfPackage(pakName);

            if (local is null) return true;
            if (webV is null) return false;

            if (webV == local) return false;
            if (webV > local) return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }


        return false;
    }

    public static async Task<Update> DownloadUpdateInfo(string pakName)
    {
        var url     = "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/Versions";
        var info    = await ServerCom.ReadTextFromURL(url);
        var version = await ServerCom.GetVersionOfPackageFromWeb(pakName);

        if (version is null) return Update.Empty;
        var update = new Update(pakName, string.Join('\n', info), version);
        return update;
    }

    public static async Task Download(string pakName)
    {
        Utilities.WriteColorText("An update was found for &g" + pakName + "&c. Version: &r" +
                                         (await ServerCom.GetVersionOfPackageFromWeb(pakName))?.ToShortString() +
                                         "&c. Current Version: &y" +
                                         ServerCom.GetVersionOfPackage(pakName)?.ToShortString());
        await ConsoleCommandsHandler.ExecuteCommad("dwplug " + pakName);
    }
}