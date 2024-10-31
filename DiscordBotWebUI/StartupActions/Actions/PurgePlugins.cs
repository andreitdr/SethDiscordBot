namespace DiscordBotWebUI.StartupActions.Actions;

public class PurgePlugins : IStartupAction
{
    public string Command => "purge_plugins";
    public void RunAction(string[] args)
    {
        foreach (var plugin in Directory.GetFiles("./Data/Plugins", "*.dll", SearchOption.AllDirectories))
        {
            File.Delete(plugin);
        }

        File.Delete("./Data/Resources/plugins.json");
        Directory.Delete("./Libraries/", true);
    }
}
