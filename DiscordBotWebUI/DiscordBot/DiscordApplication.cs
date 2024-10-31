using DiscordBotCore;
using DiscordBotCore.Bot;
using DiscordBotCore.Loaders;
using DiscordBotCore.Others;

namespace DiscordBotWebUI.DiscordBot;

public class DiscordApplication
{
    public bool IsRunning { get; private set; }
    private Action<string, LogType> LogWriter { get; set; }
    public DiscordApplication(Action<string, LogType> logWriter)
    {
        this.LogWriter = logWriter;
        IsRunning = false;
    }
    
    private async Task<bool> LoadComponents()
    {
        if (!Application.IsRunning)
        {
            await Application.CreateApplication();
        }
        
        Application.CurrentApplication.Logger.SetOutFunction(LogWriter);

        return Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("ServerID") &&
               Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("token") &&
               Application.CurrentApplication.ApplicationEnvironmentVariables.ContainsKey("prefix");

    }
    
    public async Task<bool> Start()
    {
        if (!await LoadComponents())
        {
            return false;
        }
        
        var token  = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("token");
        var prefix = Application.CurrentApplication.ApplicationEnvironmentVariables.Get<string>("prefix");

        var coreApplication = new DiscordBotApplication(token, prefix);
        await coreApplication.StartAsync();
        
        IsRunning = true;

        return true;
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
            Application.CurrentApplication.Logger.Log($"Command {command.Command} loaded successfully", LogType.Info);
        };

        loader.OnEventLoaded += (eEvent) => {
            Application.CurrentApplication.Logger.Log($"Event {eEvent.Name} loaded successfully", LogType.Info);
        };

        loader.OnActionLoaded += (action) => {
            Application.CurrentApplication.Logger.Log($"Action {action.ActionName} loaded successfully", LogType.Info);
        };

        loader.OnSlashCommandLoaded += (slashCommand) => {
            Application.CurrentApplication.Logger.Log($"Slash Command {slashCommand.Name} loaded successfully", LogType.Info);
        };

        await loader.LoadPlugins();
    }
    
    private async Task InitializeInternalActionManager()
    {
        await Application.CurrentApplication.InternalActionManager.Initialize();
    }
}
