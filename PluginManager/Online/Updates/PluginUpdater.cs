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
            var webV = await ServerCom.GetVersionOfPackageFromWeb(pakName);
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

    public static async Task Download(string pakName)
    {
        Utilities.WriteColorText("An update was found for &g" + pakName + "&c. Version: &r" +
                                         (await ServerCom.GetVersionOfPackageFromWeb(pakName))?.ToShortString() +
                                         "&c. Current Version: &y" +
                                         ServerCom.GetVersionOfPackage(pakName)?.ToShortString());
        await ConsoleCommandsHandler.ExecuteCommad("dwplug " + pakName);
    }
}