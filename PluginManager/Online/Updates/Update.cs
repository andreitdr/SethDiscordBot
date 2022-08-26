using PluginManager.Online.Helpers;

namespace PluginManager.Online.Updates
{
    public class Update
    {
        public static Update Empty = new Update(null, null, null);
        public string pakName;
        public string UpdateMessage;

        public VersionString newVersion;

        private bool isEmpty;

        public Update(string pakName, string updateMessage, VersionString newVersion)
        {
            this.pakName = pakName;
            UpdateMessage = updateMessage;
            this.newVersion = newVersion;

            if (pakName is null && updateMessage is null && newVersion is null)
                isEmpty = true;

        }

        public override string ToString()
        {
            if (isEmpty)
                throw new System.Exception("The update is EMPTY. Can not print information about an empty update !");
            return $"Package Name: {this.pakName}\n" +
                $"Update Message: {UpdateMessage}\n" +
                $"Version: {newVersion.ToString()}";
        }

    }
}
