using DiscordBotCore;
using DiscordBotCore.Bot;
using DiscordBotCore.Loaders;
using DiscordBotCore.Others;
using DiscordBotCore.Others.Exceptions;

namespace DiscordBotWebUI.DiscordBot;

public class DiscordApplication
{
    public bool IsRunning { get; private set; }
    private Action<string, LogType> LogWriter { get; set; }
    private Func<ModuleRequirement, Task> RequirementsSolver { get; set; }
    
    public DiscordApplication(Action<string, LogType> logWriter, Func<ModuleRequirement, Task> requirementsSolver)
    {
        this.LogWriter = logWriter;
        this.RequirementsSolver = requirementsSolver;
        IsRunning = false;
        
    }
    
    private async Task<bool> LoadComponents()
    {
        await Application.CreateApplication(RequirementsSolver);
        Application.Logger.SetOutFunction(LogWriter);

        return Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID") &&
               Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token") &&
               Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix");

    }
    
    public async Task Start()
    {
        if (!await LoadComponents())
        {
            Application.Logger.Log("Some required components are missing", LogType.Critical);
            return;
        }
        
        var token  = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("token");
        var prefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("prefix");

        var coreApplication = new DiscordBotApplication(token, prefix);
        await coreApplication.StartAsync();

        await RefreshPlugins(false);
        
        IsRunning = true;
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

        loader.OnCommandLoaded += (command) => {
            Application.Logger.Log($"Command {command.Command} loaded successfully", LogType.Info);
        };

        loader.OnEventLoaded += (eEvent) => {
            Application.Logger.Log($"Event {eEvent.Name} loaded successfully", LogType.Info);
        };

        loader.OnActionLoaded += (action) => {
            Application.Logger.Log($"Action {action.ActionName} loaded successfully", LogType.Info);
        };

        loader.OnSlashCommandLoaded += (slashCommand) => {
            Application.Logger.Log($"Slash Command {slashCommand.Name} loaded successfully", LogType.Info);
        };

        await loader.LoadPlugins();
    }
    
    private async Task InitializeInternalActionManager()
    {
        await Application.CurrentApplication.InternalActionManager.Initialize();
    }
}
