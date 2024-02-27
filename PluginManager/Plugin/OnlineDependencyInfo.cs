namespace PluginManager.Plugin;

public class OnlineDependencyInfo
{
    public string DownloadLink { get; private set; }
    public string DownloadLocation { get; private set; }

    public OnlineDependencyInfo(string downloadLink, string downloadLocation)
    {
        DownloadLink     = downloadLink;
        DownloadLocation = downloadLocation;
    }
}
