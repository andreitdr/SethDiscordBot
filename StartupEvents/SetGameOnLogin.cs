public class SetGameOnLogin : PluginManager.Interfaces.DBEvent
{
    public string name => "Set Game on Startup";
    public string description => "Set Custom Game to the bot at initialization";
    public async void Start(Discord.WebSocket.DiscordSocketClient client)
    {
        string UtilsPath = PluginManager.Others.Functions.dataFolder + "StartupEvents/";
        string ConfigFile = UtilsPath + "LoginEvent.txt";

        System.IO.Directory.CreateDirectory(UtilsPath);
        if (!System.IO.File.Exists(ConfigFile))
        {
            System.Console.WriteLine($"First time setup. Open file: {ConfigFile} to change settings or use the following commands\nNote: For space ( ) use underline (_). Example: 'Hello_World' will output 'Hello World'");
            System.Console.WriteLine($"set-setting StartupEvents.LoginEvent.Title [Custom_Title(s)]");
            System.Console.WriteLine($"set-setting StartupEvents.LoginEvent.Dynamic_Title [True/False]");
            System.Console.WriteLine($"set-setting StartupEvents.LoginEvent.Dynamic_Title_Change_Rate [interval in milliseconds]");
            await System.IO.File.WriteAllTextAsync(ConfigFile, "Enabled=True\n\nDynamic Title=False\n#For dynamic title add titles like this:\n#Title=Hello,World,Test,Test2\nTitle=!help\nDynamic Title Change Rate=3500\n");
        }

        if (PluginManager.Others.Functions.readCodeFromFile(ConfigFile, "Enabled", '=') != "True")
            return;

        bool isDynamic = PluginManager.Others.Functions.readCodeFromFile(ConfigFile, "Dynamic Title", '=') == "True";
        string Title = PluginManager.Others.Functions.readCodeFromFile(ConfigFile, "Title", '=');
        if (Title == null || Title.Length < 2) return;
        if (!isDynamic)
            await client.SetGameAsync(Title, null, Discord.ActivityType.Playing);
        else
        {
            string[] Titles = Title.Split(',');
            int delayMS = 3500;
            try
            {
                delayMS = int.Parse(PluginManager.Others.Functions.readCodeFromFile(ConfigFile, "Dynamic Title Change Rate", '='));
            }
            catch { }
            while (true)
            {
                foreach (var title in Titles)
                {
                    await client.SetGameAsync(title, null, Discord.ActivityType.Playing);
                    await System.Threading.Tasks.Task.Delay(delayMS);
                }
            }
        }

    }
}