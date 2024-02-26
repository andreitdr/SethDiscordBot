using System.Text.Json.Serialization;
using PluginManager.Interfaces.Updater;

namespace PluginManager.Online.Helpers;

public class PluginVersion : Version
{
    [JsonConstructor]
    public PluginVersion(int major, int minor, int patch): base(major, minor, patch)
    {
    }
    public PluginVersion(string versionAsString): base(versionAsString)
    {
    }

    public override string ToString()
    {
        return ToShortString();
    }
}
