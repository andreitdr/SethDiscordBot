
using DiscordBotCore.Interfaces.Updater;

namespace DiscordBotCore.Updater.Application
{
    public class Update
    {
        public readonly static Update None = new Update(AppVersion.CurrentAppVersion, AppVersion.CurrentAppVersion, string.Empty, string.Empty);

        public AppVersion UpdateVersion { get; private set; }
        public AppVersion CurrentVersion { get; private set; }
        public string UpdateUrl { get; private set; }
        public string UpdateNotes { get; private set; }

        public Update(AppVersion currentVersion, AppVersion updateVersion, string updateUrl, string updateNotes)
        {
            UpdateVersion = updateVersion;
            CurrentVersion = currentVersion;
            UpdateUrl = updateUrl;
            UpdateNotes = updateNotes;
        }
    }
}
