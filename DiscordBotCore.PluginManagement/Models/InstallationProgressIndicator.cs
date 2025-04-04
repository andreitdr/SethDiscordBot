namespace DiscordBotCore.PluginManagement.Models;

public class InstallationProgressIndicator
{
    private readonly Dictionary<string, float> _DownloadProgress;
    
    public InstallationProgressIndicator()
    {
        _DownloadProgress = new Dictionary<string, float>();
    }
    
    public void SetProgress(string fileName, float progress)
    {
        _DownloadProgress[fileName] = progress;
    }
}