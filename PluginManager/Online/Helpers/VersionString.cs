using System;

namespace PluginManager.Online.Helpers;

public class VersionString
{
    public int PackageCheckVersion;
    public int PackageMainVersion;
    public int PackageVersionID;

    public VersionString(string version)
    {
        var data = version.Split('.');
        try
        {
            PackageVersionID = int.Parse(data[0]);
            PackageMainVersion = int.Parse(data[1]);
            PackageCheckVersion = int.Parse(data[2]);
        }
        catch (Exception ex)
        {
            Console.WriteLine(version);
            throw new Exception("Failed to write Version", ex);
        }
    }

    public override string ToString()
    {
        return "{PackageID: " + PackageVersionID + ", PackageVersion: " + PackageMainVersion +
               ", PackageCheckVersion: " + PackageCheckVersion + "}";
    }

    public string ToShortString()
    {
        if (PackageVersionID == 0 && PackageCheckVersion == 0 && PackageMainVersion == 0)
            return "Unknown";
        return $"{PackageVersionID}.{PackageMainVersion}.{PackageCheckVersion}";
    }


    #region operators

    public static bool operator >(VersionString s1, VersionString s2)
    {
        if (s1.PackageVersionID > s2.PackageVersionID) return true;
        if (s1.PackageVersionID == s2.PackageVersionID)
        {
            if (s1.PackageMainVersion > s2.PackageMainVersion) return true;
            if (s1.PackageMainVersion == s2.PackageMainVersion &&
                s1.PackageCheckVersion > s2.PackageCheckVersion) return true;
        }

        return false;
    }

    public static bool operator <(VersionString s1, VersionString s2)
    {
        return !(s1 > s2) && s1 != s2;
    }

    public static bool operator ==(VersionString s1, VersionString s2)
    {
        if (s1.PackageVersionID == s2.PackageVersionID && s1.PackageMainVersion == s2.PackageMainVersion &&
            s1.PackageCheckVersion == s2.PackageCheckVersion) return true;
        return false;
    }

    public static bool operator !=(VersionString s1, VersionString s2)
    {
        return !(s1 == s2);
    }

    public static bool operator <=(VersionString s1, VersionString s2)
    {
        return s1 < s2 || s1 == s2;
    }

    public static bool operator >=(VersionString s1, VersionString s2)
    {
        return s1 > s2 || s1 == s2;
    }

    #endregion
}