using System.Collections.Generic;
using System.Threading.Tasks;
using Discord.WebSocket;
using DiscordBotCore.Interfaces;
using DiscordBotCore.Others;


namespace DiscordBotCore.Loaders;

public class PluginLoader
{
    internal readonly DiscordSocketClient _Client;

    public delegate void CommandLoaded(PluginLoadResultData resultData);

    public delegate void EventLoaded(PluginLoadResultData resultData);

    public delegate void SlashCommandLoaded(PluginLoadResultData resultData);

    public CommandLoaded?      OnCommandLoaded;
    public EventLoaded?        OnEventLoaded;
    public SlashCommandLoaded? OnSlashCommandLoaded;

    public static List<DBCommand> Commands { get; private set; } = new List<DBCommand>();
    public static List<DBEvent>  Events { get; private set; } = new List<DBEvent>();
    public static List<DBSlashCommand> SlashCommands { get; private set; } = new List<DBSlashCommand>();

    public PluginLoader(DiscordSocketClient discordSocketClient)
    {
        _Client = discordSocketClient;
    }

    public async Task LoadPlugins()
    {
        Application.CurrentApplication.Logger.Log("Loading plugins...", typeof(PluginLoader));
        
        var loader = new Loader(Application.CurrentApplication.ApplicationEnvironmentVariables["PluginFolder"], "dll");

        //await this.ResetSlashCommands();

        loader.OnFileLoadedException += FileLoadedException;
        loader.OnPluginLoaded        += OnPluginLoaded;

        await loader.Load();
    }

    private void FileLoadedException(FileLoaderResult result)
    {
        Application.CurrentApplication.Logger.Log(result.ErrorMessage, typeof(PluginLoader), LogType.ERROR);
    }

    private async void OnPluginLoaded(PluginLoadResultData result)
    {
        switch (result.PluginType)
        {
            case PluginType.COMMAND:
                Commands.Add((DBCommand)result.Plugin);
                OnCommandLoaded?.Invoke(result);
                break;
            case PluginType.EVENT:
                if (this.TryStartEvent((DBEvent)result.Plugin))
                {
                    Events.Add((DBEvent)result.Plugin);
                    OnEventLoaded?.Invoke(result);
                }

                break;
            case PluginType.SLASH_COMMAND:
                if (await this.TryStartSlashCommand((DBSlashCommand)result.Plugin))
                {
                    if(((DBSlashCommand)result.Plugin).HasInteraction)
                        _Client.InteractionCreated += ((DBSlashCommand)result.Plugin).ExecuteInteraction;
                    SlashCommands.Add((DBSlashCommand)result.Plugin);
                    OnSlashCommandLoaded?.Invoke(result);
                } 
                else 
                    Application.CurrentApplication.Logger.Log($"Failed to start slash command {result.PluginName}", typeof(PluginLoader), LogType.ERROR);
                break;
            case PluginType.UNKNOWN:
            default:
                Application.CurrentApplication.Logger.Log("Unknown plugin type", typeof(PluginLoader), LogType.ERROR);
                break;
        }
    }
}
