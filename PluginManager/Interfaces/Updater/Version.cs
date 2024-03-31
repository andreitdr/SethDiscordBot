using System;

namespace PluginManager.Interfaces.Updater;

public abstract class Version: IVersion
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    protected readonly char _Separator = '.';

    protected Version(int major, int minor, int patch)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    protected Version(string versionAsString)
    {
        string[] versionParts = versionAsString.Split(_Separator);

        if (versionParts.Length != 3)
        {
            throw new ArgumentException("Invalid version string");
        }

        Major = int.Parse(versionParts[0]);
        Minor = int.Parse(versionParts[1]);
        Patch = int.Parse(versionParts[2]);
    }

    public bool IsNewerThan(IVersion version)
    {
        if (Major > version.Major)
            return true;

        if (Major == version.Major && Minor > version.Minor)
            return true;

        if (Major == version.Major && Minor == version.Minor && Patch > version.Patch)
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

        return false;
    }

    public bool IsEqualTo(IVersion version)
    {
        return Major == version.Major && Minor == version.Minor && Patch == version.Patch;
    }

    public string ToShortString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

    public override string ToString()
    {
        return ToShortString();
    }
}
