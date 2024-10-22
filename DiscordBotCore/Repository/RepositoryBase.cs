namespace DiscordBotCore.Repository;

public abstract class RepositoryBase
{
/*
 Example of JSON for config file :
 Please mind that the URL should not end with a slash. !
 
  "PluginRepository": {
    "Name": "Default Plugins Repository",
    "Url": "http://blahblahblah.blah/MyCustomRepo",
    "DatabaseFile": "PluginsList.json"
  },
  "ModuleRepository": {
    "Name": "Default Modules Repository",
    "Url": "http://blahblahblah.blah/MyCustomRepo",
    "DatabaseFile": "modules.json"
  }
 */
    public string RepositoryName { get; init; }
    private string RepositoryUrl { get; init; }
    private string DatabaseFile { get; init; }

    protected string DatabasePath => $"{RepositoryUrl}/{DatabaseFile}";
    
    protected RepositoryBase(string repositoryName, string repositoryUrl, string databaseFile)
    {
        RepositoryName = repositoryName;
        RepositoryUrl = repositoryUrl;
        DatabaseFile = databaseFile;
    }

}
