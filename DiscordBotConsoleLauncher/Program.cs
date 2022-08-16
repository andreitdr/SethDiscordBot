using PluginManager.Online.Updates;

try
{
    bool requireUpdate = await PluginUpdater.CheckForUpdates("DiscordBotConsoleLauncher");
    if (requireUpdate)
    {
        var update = await PluginUpdater.DownloadUpdateInfo("DiscordBotConsoleLauncher");
        if (update == Update.Empty)
            return;

        Console.WriteLine("Found an update: ");
        Console.WriteLine(update.ToString());
    }
}
catch (Exception ex)
{
    Console.WriteLine("An exception was thrown. ");
    Console.WriteLine(ex.Message);
    Environment.Exit(-2);
}