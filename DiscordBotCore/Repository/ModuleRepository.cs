using System;
using System.Threading.Tasks;
using DiscordBotCore.Online;

namespace DiscordBotCore.Repository;

public sealed class ModuleRepository : RepositoryBase
{
    public static readonly ModuleRepository Default = new ModuleRepository("Testing", "https://wizzy-server.ro/SethDiscordBot/ModulesRepo", "modules.json");
    private ModuleRepository(string repositoryName, string repositoryUrl, string databaseFile) : base(repositoryName, repositoryUrl, databaseFile)
    {
    }
    
    public async Task<string> JsonGetAllModules()
    {
        var jsonResponse = await ServerCom.GetAllTextFromUrl(DatabasePath);
        return jsonResponse;
    }
    
    private static ModuleRepository From(string repositoryName, string repositoryUrl, string databaseFile)
    {
        return new ModuleRepository(repositoryName, repositoryUrl, databaseFile);
    }
    
    internal static ModuleRepository SolveRepo()
    {
        if (!Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ModuleRepository"))
        {
            return Default;
        }
        
        try
        {
            var moduleRepoDict = Application.CurrentApplication.ApplicationEnvironmentVariables.GetDictionary<string, string>("ModuleRepository");
            var moduleRepo = From(
            moduleRepoDict["Name"],
            moduleRepoDict["Url"],
            moduleRepoDict["DatabaseFile"]
            );
            return moduleRepo;
        }
        catch(Exception ex)
        {
            Application.Logger.LogException(ex, Application.CurrentApplication);

            return Default;
        }
    }
}
