using System;

namespace PluginManager.Interfaces.Updater;

public abstract class Version : IVersion
{
    public int Major { get; }
    public int Minor { get; }
    public int Patch { get; }

    protected readonly char _Separator = '.';

    protected Version(int major, int minor, int patch)
    {
        this.Major = major;
        this.Minor = minor;
        this.Patch = patch;
    }

    protected Version(string versionAsString)
    {
        string[] versionParts = versionAsString.Split(_Separator);
        
        if (versionParts.Length != 3)
        {
            throw new ArgumentException("Invalid version string");
        }
        
        this.Major = int.Parse(versionParts[0]);
        this.Minor = int.Parse(versionParts[1]);
        this.Patch = int.Parse(versionParts[2]);
    }
    
    public bool IsNewerThan(IVersion version)
    {
        if (this.Major > version.Major)
            return true;

        if (this.Major == version.Major && this.Minor > version.Minor)
            return true;

        if (this.Major == version.Major && this.Minor == version.Minor && this.Patch > version.Patch)
            return true;

        return false;
    }

    public bool IsOlderThan(IVersion version)
    {
        if (this.Major < version.Major)
            return true;

        if (this.Major == version.Major && this.Minor < version.Minor)
            return true;

        if (this.Major == version.Major && this.Minor == version.Minor && this.Patch < version.Patch)
            return true;

        return false;
    }

    public bool IsEqualTo(IVersion version)
    {
        return this.Major == version.Major && this.Minor == version.Minor && this.Patch == version.Patch;
    }
    
    public string ToShortString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }
}
