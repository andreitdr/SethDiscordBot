using System.IO;

using PluginManager.Database;

namespace PluginManager
{
    public class Settings
    {

        public static class Variables
        {
            public static string WebsiteURL = "https://wizzy69.github.io/SethDiscordBot";
            public static string UpdaterURL = "https://github.com/Wizzy69/installer/releases/download/release-1-discordbot/Updater.zip";

            public static TextWriter outputStream;
        }

        public static SqlDatabase sqlDatabase;
    }

}
