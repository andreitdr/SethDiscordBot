using System;
using System.Reflection;

namespace DiscordBotCore.Interfaces.Updater
{
    public class AppVersion : IVersion
    {
        public int Major { get; set; }

        public int Minor  { get; set; }

        public int Patch { get; set; }

        public int PatchVersion { get; set; }

        public static readonly AppVersion CurrentAppVersion = new AppVersion(Assembly.GetEntryAssembly().GetName().Version.ToString());

        private readonly char _Separator = '.';

        public AppVersion(string versionAsString)
        {
            string[] versionParts = versionAsString.Split(_Separator);

            if (versionParts.Length != 4)
            {
                throw new ArgumentException("Invalid version string");
            }

            Major = int.Parse(versionParts[0]);
            Minor = int.Parse(versionParts[1]);
            Patch = int.Parse(versionParts[2]);
            PatchVersion = int.Parse(versionParts[3]);
        }

        public bool IsNewerThan(IVersion version)
        {
            if (Major > version.Major)
                return true;

            if (Major == version.Major && Minor > version.Minor)
                return true;

            if (Major == version.Major && Minor == version.Minor && Patch > version.Patch)
                return true;

            if (Major == version.Major && Minor == version.Minor && Patch == version.Patch && PatchVersion > version.PatchVersion)
                return true;

            return false;
        }

        public bool IsOlderThan(IVersion version)
        {
            if (Major < version.Major)
                return true;

            if (Major == version.Major && Minor < version.Minor)
                return true;

            if (Major == version.Major && Minor == version.Minor && Patch < version.Patch)
                return true;

            if (Major == version.Major && Minor == version.Minor && Patch == version.Patch && PatchVersion < version.PatchVersion)
                return true;

            return false;
        }

        public bool IsEqualTo(IVersion version)
        {
            return Major == version.Major && Minor == version.Minor && Patch == version.Patch && PatchVersion == version.PatchVersion;
        }

        public string ToShortString()
        {
            return $"{Major}.{Minor}.{Patch}.{PatchVersion}";
        }

        public override string ToString()
        {
            return ToShortString();
        }
    }
}
