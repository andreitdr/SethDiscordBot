using PluginManager.Interfaces.Updater;

namespace PluginManager.Online.Helpers;

public class ApplicationVersion : Version
{

    public ApplicationVersion(int major, int minor, int patch): base(major, minor, patch)
    {
    }
    public ApplicationVersion(string versionAsString): base(versionAsString)
    {
    }
    
    
}
