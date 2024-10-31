namespace DiscordBotWebUI.StartupActions.Actions;

public class UpdateCleanup : IStartupAction
{
    public string Command => "update-cleanup";
    
    public void RunAction(string[] args)
    {
        List<string> files = new List<string>();
        files.AddRange(Directory.GetFiles("./"));

        foreach (var file in files)
        {
            if (file.EndsWith(".bak"))
                File.Delete(file);
        }

        Directory.Delete("temp");
    }
}
