
using DiscordBotCore.Interfaces.Updater;

namespace DiscordBotCore.Updater.Application
{
    public class Update
    {
        public AppVersion NewVersion { get; private set; }
        public AppVersion CurrentVersion { get; private set; }
        public string UpdateUrl { get; private set; }

        public Update(AppVersion currentVersion, AppVersion updateVersion, string updateUrl)
        {
            NewVersion = updateVersion;
            CurrentVersion = currentVersion;
            UpdateUrl = updateUrl;
        }
    }
}
