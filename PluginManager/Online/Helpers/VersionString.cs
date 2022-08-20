using PluginManager.Others;

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace PluginManager.Online.Helpers
{
    public class VersionString
    {
        public int PackageID;
        public int PackageMainVersion;
        public int PackageCheckVersion;

        public VersionString(string version)
        {
            string[] data = version.Split('.');
            try
            {
                PackageID = int.Parse(data[0]);
                PackageMainVersion = int.Parse(data[1]);
                PackageCheckVersion = int.Parse(data[2]);
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to write Version", ex);
            }
        }

        public static bool operator >(VersionString s1, VersionString s2)
        {
            if (s1.PackageID != s2.PackageID) throw new Exception("Can not compare two different paks");
            if (s1.PackageMainVersion > s2.PackageMainVersion) return true;
            if (s1.PackageMainVersion == s2.PackageMainVersion && s1.PackageCheckVersion > s2.PackageCheckVersion) return true;

            return false;
        }

        #region operators
        public static bool operator <(VersionString s1, VersionString s2) => !(s1 > s2) && s1 != s2;

        public static bool operator ==(VersionString s1, VersionString s2)
        {
            if (s1.PackageID == s2.PackageID && s1.PackageMainVersion == s2.PackageMainVersion && s1.PackageCheckVersion == s2.PackageCheckVersion) return true;
            return false;
        }

        public static bool operator !=(VersionString s1, VersionString s2) => !(s1 == s2);

        public static bool operator <=(VersionString s1, VersionString s2) => (s1 < s2 || s1 == s2);
        public static bool operator >=(VersionString s1, VersionString s2) => (s1 > s2 || s1 == s2);

        #endregion

        public override string ToString()
        {
            return "{PackageID: " + PackageID + ", PackageVersion: " + PackageMainVersion + ", PackageCheckVersion: " + PackageCheckVersion + "}";
        }

        public string ToShortString()
        {
            return $"{PackageID}.{PackageMainVersion}.{PackageCheckVersion}";
        }

        public static VersionString? GetVersionOfPackage(string pakName)
        {
            if (!Config.PluginVersionsContainsKey(pakName))
                return null;
            return new VersionString(Config.GetPluginVersion(pakName));
        }

        public static async Task<VersionString?> GetVersionOfPackageFromWeb(string pakName)
        {
            string url = "https://raw.githubusercontent.com/Wizzy69/installer/discord-bot-files/Versions";
            List<string> data = await ServerCom.ReadTextFromURL(url);
            string? version = (from item in data
                               where !item.StartsWith("#") && item.StartsWith(pakName)
                               select item.Split(',')[1]).FirstOrDefault();
            if (version == default || version == null) return null;
            return new VersionString(version);
        }

    }
}
