using DiscordBotCore;
using DiscordBotCore.Bot;
using DiscordBotCore.Loaders;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;

namespace DiscordBotWebUI.DiscordBot;

public class DiscordBotStartup
{
    public event EventHandler<string>? Log; 
    private void WriteLog(string message)
    {
        Log?.Invoke(this, message);
    }

    private readonly Func<ModuleRequirement, Task> RequireInstallModule;
    
    public DiscordBotStartup(Func<ModuleRequirement, Task> requirementHandler)
    {
        this.RequireInstallModule = requirementHandler;
    }
    
    public async Task CreateApplication()
    {
        await Application.CreateApplication(RequireInstallModule);
    }
    
    public bool LoadComponents()
    {
        Application.Logger.SetOutFunction(WriteLog);

        return Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID") &&
               Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token") &&
               Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix");

    }
    
    public async Task PrepareBot()
    {
        var token  = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("token");
        var prefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("prefix");

        DiscordBotApplication app = new DiscordBotApplication(token, prefix);
        await app.StartAsync();
    }
    
    public async Task RefreshPlugins(bool quiet)
    {
        await LoadPlugins(quiet ? ["-q"] : null);
        await InitializeInternalActionManager();
    }
    
    private async Task LoadPlugins(string[]? args)
    {
        var loader = new PluginLoader(Application.CurrentApplication.DiscordBotClient.Client);
        if (args is not null && args.Contains("-q"))
        {
            await loader.LoadPlugins();
        }
        
        loader.OnCommandLoaded += (command) =>
        {
            Application.Logger.Log($"Command {command.Command} loaded successfully", LogType.Info);
        };
        
        loader.OnEventLoaded += (eEvent) =>
        {
            Application.Logger.Log($"Event {eEvent.Name} loaded successfully",LogType.Info);
        };
        
        loader.OnActionLoaded += (action) =>
        {
            Application.Logger.Log($"Action {action.ActionName} loaded successfully", LogType.Info);
        };
        
        loader.OnSlashCommandLoaded += (slashCommand) =>
        {
            Application.Logger.Log($"Slash Command {slashCommand.Name} loaded successfully", LogType.Info);
        };

        await loader.LoadPlugins();
    }
    
    private async Task InitializeInternalActionManager()
    {
        await Application.CurrentApplication.InternalActionManager.Initialize();
    }
    
}
