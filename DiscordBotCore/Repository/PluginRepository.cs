using System;
using System.Threading.Tasks;
using DiscordBotCore.Online;

namespace DiscordBotCore.Repository;

public sealed class PluginRepository : RepositoryBase
{
    public static readonly PluginRepository Default = new PluginRepository("Testing", "https://files.wizzy-server.ro/SethDiscordBot/PluginsRepo", "PluginsList.json");

    private PluginRepository(string repositoryName, string repositoryUrl, string databaseFile) : base(repositoryName, repositoryUrl, databaseFile)
    {
    }
    
    private static PluginRepository From(string repositoryName, string repositoryUrl, string databaseFile)
    {
        return new PluginRepository(repositoryName, repositoryUrl, databaseFile);
    }

    public async Task<string> JsonGetAllPlugins()
    {
        var jsonResponse = await ServerCom.GetAllTextFromUrl(DatabasePath);
        
        return jsonResponse;
    }

    internal static PluginRepository SolveRepo()
    {
        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("PluginRepository"))
        {
            return Default;
        }
        
        try
        {
            var pluginRepoDict = Application.CurrentApplication.ApplicationEnvironmentVariables.GetDictionary<string, string>("PluginRepository");
            var pluginRepo = PluginRepository.From(
            pluginRepoDict["Name"],
            pluginRepoDict["Url"],
            pluginRepoDict["DatabaseFile"]
            );
            return pluginRepo;
        }
        catch(Exception ex)
        {
            Application.CurrentApplication.Logger.LogException(ex, Application.CurrentApplication);

            return Default;
        }
    }
}
